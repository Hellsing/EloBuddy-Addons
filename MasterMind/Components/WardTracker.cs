using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;
using EloBuddy.SDK.Notifications;
using EloBuddy.SDK.Rendering;
using MasterMind.Properties;
using SharpDX;
using SharpDX.Direct3D9;
using Color = System.Drawing.Color;
using Line = EloBuddy.SDK.Rendering.Line;
using RectangleF = SharpDX.RectangleF;

namespace MasterMind.Components
{
    public sealed class WardTracker : IComponent
    {
        public static Dictionary<Ward.Type, CheckBox> EnabledWards { get; private set; }
        private static readonly List<Obj_AI_Base> CreatedWards = new List<Obj_AI_Base>();
        public static Ward.PinkColors PinkColor { get; private set; }

        private static readonly Dictionary<string, Ward.Type> SpellToWardDictionary = new Dictionary<string, Ward.Type>
        {
            { "TrinketTotemLvl1", Ward.Type.YellowTrinket },
            { "VisionWard", Ward.Type.VisionWard },
            { "ItemGhostWard", Ward.Type.SightWard },
            { "TrinketOrbLvl3", Ward.Type.BlueTrinket }
        };

        public Menu Menu { get; private set; }

        public CheckBox RenderWard { get; private set; }
        public CheckBox DrawMinimap { get; private set; }
        public CheckBox DrawHealth { get; private set; }
        public CheckBox DrawTime { get; private set; }

        public static CheckBox NotifyPlace { get; private set; }
        public static CheckBox NotifyPlacePing { get; private set; }
        public static Slider NotifyRange { get; private set; }

        private List<Ward> ActiveWards { get; set; }

        public bool ShouldLoad(bool isSpectatorMode = false)
        {
            // Always load, regardless the game mode
            return true;
        }

        public void InitializeComponent()
        {
            // Initialize menu
            Menu = MasterMind.Menu.AddSubMenu("Ward Tracker", longTitle: "Ward Presence Tracker");

            Menu.AddGroupLabel("Information");
            Menu.AddLabel("A ward presence tracker helps you in various ways ingame.");
            Menu.AddLabel("It lets you visually see where different ward types have been placed,");
            Menu.AddLabel("even when you were not inrage or didn't see the ward on creation.");

            // Add all known ward types to the menu
            Menu.AddSeparator();
            Menu.AddLabel("You can enable ward tracking for the following ward types:");
            EnabledWards = new Dictionary<Ward.Type, CheckBox>();
            foreach (var wardType in Enum.GetValues(typeof (Ward.Type)).Cast<Ward.Type>())
            {
                EnabledWards[wardType] = Menu.Add(wardType.ToString(), new CheckBox(wardType.ToString()));
            }

            // World options
            Menu.AddSeparator();
            Menu.AddLabel("World options:");
            RenderWard = Menu.Add("renderWard", new CheckBox("Render ward on map (circle)"));
            DrawHealth = Menu.Add("drawHealth", new CheckBox("Draw remaining ward health"));
            DrawTime = Menu.Add("drawTime", new CheckBox("Draw remaining ward time"));

            // Minimap options
            Menu.AddSeparator();
            Menu.AddLabel("Minimap options:");
            DrawMinimap = Menu.Add("drawMinimap", new CheckBox("Draw ward icon"));
            if (!MasterMind.IsSpectatorMode)
            {
                var pinkColor = new ComboBox("Pink ward color", Enum.GetValues(typeof (Ward.PinkColors)).Cast<Ward.PinkColors>().Select(o => o.ToString()));
                pinkColor.OnValueChange += delegate(ValueBase<int> sender, ValueBase<int>.ValueChangeArgs args) { PinkColor = (Ward.PinkColors) args.NewValue; };
                Menu.Add("pinkColor", pinkColor);
            }

            if (!MasterMind.IsSpectatorMode)
            {
                // Notification options
                Menu.AddSeparator();
                Menu.AddLabel("Notification options:");
                NotifyPlace = Menu.Add("notifyPlaceNear", new CheckBox("Notify when enemy nearby places a ward"));
                NotifyPlacePing = Menu.Add("notifyPlaceNearPing", new CheckBox("Notify with local ping", false));
                NotifyRange = Menu.Add("notifyPlaceNearRange", new Slider("Notification altert range", 2000, 500, 5000));
            }

            // Initialize properties
            ActiveWards = new List<Ward>();

            // Get all currently known wards
            ObjectManager.Get<Obj_AI_Minion>().ToList().ForEach(o => OnCreate(o, EventArgs.Empty));

            // Listen to required events
            Game.OnTick += OnTick;
            Drawing.OnDraw += OnDraw;
            Drawing.OnEndScene += OnEndScene;
            GameObject.OnCreate += OnCreate;
            GameObject.OnDelete += OnDelete;
            Obj_AI_Base.OnPlayAnimation += OnAnimation;
            Obj_AI_Base.OnBuffGain += OnBuffGain;
            Obj_AI_Base.OnProcessSpellCast += OnProcessSpellCast;
        }

        private void OnTick(EventArgs args)
        {
            foreach (var fakeWard in ActiveWards.Where(o => o.IsFakeWard).ToArray())
            {
                switch (fakeWard.WardType)
                {
                    case Ward.Type.SightWard:
                    case Ward.Type.YellowTrinket:

                        if (fakeWard.RemainingTime <= 0)
                        {
                            // Remove timed up fake wards
                            ActiveWards.Remove(fakeWard);
                        }
                        break;
                }
            }
        }

        private void OnDraw(EventArgs args)
        {
            ActiveWards.ForEach(ward =>
            {
                // Check if ward type is enabled
                if (ward.IsEnabled)
                {
                    // Actions for invisible wards
                    if (!ward.IsVisible)
                    {
                        if (RenderWard.CurrentValue)
                        {
                            // Render a fake ward object on the map (soon)
                            ward.RenderWard();
                        }
                        if (DrawHealth.CurrentValue)
                        {
                            // Draw the remaining ward health on the map
                            ward.DrawHealth();
                        }
                    }

                    // Draw the remaining ward time for all wards
                    ward.DrawTime();
                }
            });
        }

        private void OnEndScene(EventArgs args)
        {
            if (DrawMinimap.CurrentValue)
            {
                foreach (var ward in ActiveWards.Where(o => !o.IsVisible && o.IsEnabled))
                {
                    // Draw the ward icon on the minimap for invisible wards
                    ward.DrawMinimap();
                }
            }
        }

        private void OnCreate(GameObject sender, EventArgs args)
        {
            // Check if the sender is a minion object
            if (sender.Type == GameObjectType.obj_AI_Minion)
            {
                var baseSender = (Obj_AI_Base) sender;

                // Try to parse the ward
                Ward.Type wardType;
                if (Ward.IsWard(baseSender, out wardType))
                {
                    // Add ward to created wards
                    CreatedWards.Add(baseSender);

                    // Validate all buffs of the ward
                    baseSender.Buffs.ForEach(o => OnBuffGain(baseSender, new Obj_AI_BaseBuffGainEventArgs(o)));
                }
            }
        }

        private void OnDelete(GameObject sender, EventArgs args)
        {
            // Check if the sender is a minion object
            if (sender.Type == GameObjectType.obj_AI_Minion)
            {
                // Remove it from the lists
                CreatedWards.Remove((Obj_AI_Base) sender);
                ActiveWards.RemoveAll(o => !o.IsFakeWard && o.Handle.IdEquals(sender));
            }
        }

        private void OnAnimation(Obj_AI_Base sender, GameObjectPlayAnimationEventArgs args)
        {
            // Check if sender is an active ward
            if (sender.Type == GameObjectType.obj_AI_Minion)
            {
                var ward = ActiveWards.Find(o => o.Handle.IdEquals(sender));
                if (ward != null)
                {
                    switch (args.Animation)
                    {
                        case "Death":
                            // Remove the ward
                            OnDelete(sender, EventArgs.Empty);
                            break;
                    }
                }
            }
        }

        private void OnBuffGain(Obj_AI_Base sender, Obj_AI_BaseBuffGainEventArgs args)
        {
            // Only check minion types
            if (sender.Type != GameObjectType.obj_AI_Minion ||
                (args.Buff.Name != "sharedwardbuff" &&
                 args.Buff.Name != "sharedstealthwardbuff" &&
                 args.Buff.Name != "sharedvisionwardbuff"))
            {
                return;
            }

            if (CreatedWards.Contains(sender))
            {
                // Check if there is already a fake ward at that position
                var fakeWard = ActiveWards.Where(o => o.IsFakeWard && o.Team == sender.Team)
                    .Where(
                        o => o.Position.IsInRange(sender, 500) &&
                             (args.Buff.EndTime - Game.Time > 1000
                                 ? o.RemainingTime < 0
                                 : Math.Abs(o.RemainingTime - (args.Buff.EndTime - Game.Time)) < 3))
                    .OrderBy(o => Math.Abs(o.RemainingTime - (args.Buff.EndTime - Game.Time)))
                    .ThenBy(o => o.Position.Distance(sender, true))
                    .FirstOrDefault();
                if (fakeWard != null)
                {
                    // Remove the ward from the created wards list
                    CreatedWards.Remove(sender);

                    // Turn ward into a real ward
                    fakeWard.Handle = sender;
                }
                else
                {
                    // Get the caster of the ward
                    if (args.Buff.Caster != null && args.Buff.Caster.Type == GameObjectType.AIHeroClient)
                    {
                        var caster = EntityManager.Heroes.AllHeroes.Find(o => o.IdEquals(args.Buff.Caster));
                        if (caster != null)
                        {
                            // Create a new ward wrapper
                            var ward = Ward.ParseObject(caster, sender);
                            if (ward != null)
                            {
                                // Remove the ward from the created wards list
                                CreatedWards.Remove(sender);

                                // Add the ward to the active wards list
                                ActiveWards.Add(ward);
                            }
                        }
                    }
                }
            }

            var activeWard = ActiveWards.Find(o => o.Handle.IdEquals(sender));
            if (activeWard != null)
            {
                // Check for active buffs
                CheckBuffs(activeWard);
            }
        }

        private void OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (sender.Type == GameObjectType.AIHeroClient && SpellToWardDictionary.ContainsKey(args.SData.Name))
            {
                // Create a fake ward at that position
                ActiveWards.Add(Ward.FromSpellCast((AIHeroClient) sender, SpellToWardDictionary[args.SData.Name], args.End));
            }
        }

        private static void CheckBuffs(Ward ward)
        {
            if (ward.RemainingTimeDelegate == null)
            {
                // Get the correct buff name (seems like this one is used for all of them)
                const string buffName = "sharedwardbuff";

                // Check for remaining time buff
                var buff = ward.Handle.Buffs.Find(o => o.Name == buffName);
                if (buff != null)
                {
                    // Apply remaining time delegate
                    ward.RemainingTimeDelegate = () => (int) Math.Ceiling(buff.EndTime - Game.Time);
                }
            }
        }

        public class Ward
        {
            public const int Radius = 65;

            private static readonly Vector2 HealthBarSize = new Vector2(64, 6);
            private static readonly RectangleF HealthBar = new RectangleF(-HealthBarSize.X / 2, -HealthBarSize.Y / 2, HealthBarSize.X, HealthBarSize.Y);

            private const int HealthBarBorderWidth = 1;
            private const int HealthBarPadding = 2;
            private static readonly Color HealthBarBackgroundColor = Color.Black;
            private static readonly Dictionary<GameObjectTeam, Tuple<Color, Color>> HealthBarColors = new Dictionary<GameObjectTeam, Tuple<Color, Color>>
            {
                { GameObjectTeam.Order, new Tuple<Color, Color>(Color.FromArgb(81, 162, 230), Color.FromArgb(40, 80, 114)) },
                { GameObjectTeam.Chaos, new Tuple<Color, Color>(Color.FromArgb(230, 104, 104), Color.FromArgb(114, 51, 51)) }
            };

            public enum Type
            {
                BlueTrinket = 3,
                SightWard = 0,
                VisionWard = 1,
                YellowTrinket = 2
            }

            public enum PinkColors
            {
                Pink,
                Red
            }

            static Ward()
            {
                // Load the textures
                MasterMind.TextureLoader.Load("BlueTrinket", Resources.BlueTrinket);
                MasterMind.TextureLoader.Load("VisionWard", Resources.VisionWard);
                MasterMind.TextureLoader.Load("VisionWard_Enemy", Resources.VisionWard_Enemy);
                MasterMind.TextureLoader.Load("VisionWard_Friendly", Resources.VisionWard_Friendly);
                MasterMind.TextureLoader.Load("YellowTrinket_Enemy", Resources.YellowTrinket_Enemy);
                MasterMind.TextureLoader.Load("YellowTrinket_Friendly", Resources.YellowTrinket_Friendly);
            }

            #region Constructor Properties

            public AIHeroClient Caster { get; private set; }
            public Obj_AI_Base Handle { get; set; }
            public Vector3 FakePosition { get; private set; }
            public Type WardType { get; private set; }
            public int Duration { get; private set; }
            public int CreationTime { get; private set; }

            #endregion

            #region Wrapped Properties

            public Vector3 Position
            {
                get { return Handle != null ? Handle.Position : FakePosition; }
            }
            public Vector2 ScreenPosition
            {
                get { return Handle != null ? Handle.Position.WorldToScreen() : FakePosition.WorldToScreen(); }
            }
            public Vector2 MinimapPosition
            {
                get { return Handle != null ? Handle.Position.WorldToMinimap() : FakePosition.WorldToMinimap(); }
            }
            public bool IsVisible
            {
                get { return Handle != null && Handle.IsHPBarRendered; }
            }
            private GameObjectTeam _team;
            public GameObjectTeam Team
            {
                get { return Handle != null ? Handle.Team : _team; }
                set { _team = value; }
            }
            public float MaxHealth
            {
                get
                {
                    switch (WardType)
                    {
                        case Type.BlueTrinket:
                            return 1;
                        case Type.VisionWard:
                            return 5;
                        default:
                            return 3;
                    }
                }
            }

            #endregion

            public bool IsFakeWard
            {
                get { return Handle == null; }
            }

            private EloBuddy.SDK.Rendering.Sprite MinimapSprite { get; set; }
            private Text TextHandle { get; set; }

            private Texture MinimapIconTexture
            {
                get
                {
                    switch (WardType)
                    {
                        // Blue trinkets
                        case Type.BlueTrinket:
                            return MasterMind.TextureLoader["BlueTrinket"];
                        // Pink wards
                        case Type.VisionWard:
                            return (MasterMind.IsSpectatorMode ? Team == GameObjectTeam.Order : Team.IsAlly())
                                ? MasterMind.TextureLoader["VisionWard_Friendly"]
                                : (MasterMind.IsSpectatorMode
                                    ? MasterMind.TextureLoader["VisionWard_Enemy"]
                                    : MasterMind.TextureLoader[PinkColor == PinkColors.Pink ? "VisionWard" : "VisionWard_Enemy"]);
                        // All other regular wards
                        default:
                            return (MasterMind.IsSpectatorMode ? Team == GameObjectTeam.Order : Team.IsAlly())
                                ? MasterMind.TextureLoader["YellowTrinket_Friendly"]
                                : MasterMind.TextureLoader["YellowTrinket_Enemy"];
                    }
                }
            }

            public bool IsEnabled
            {
                get { return EnabledWards.ContainsKey(WardType) && EnabledWards[WardType].CurrentValue; }
            }

            public Func<int> RemainingTimeDelegate { get; set; }
            public int RemainingTime
            {
                get
                {
                    switch (WardType)
                    {
                        case Type.SightWard:
                        case Type.YellowTrinket:
                            return
                                IsVisible
                                    ? (int) Handle.Mana
                                    : (RemainingTimeDelegate != null
                                        ? RemainingTimeDelegate()
                                        : (((CreationTime + Duration) - Core.GameTickCount) / 1000));
                    }
                    return -1;
                }
            }
            public string RemainingTimeText
            {
                get
                {
                    var time = RemainingTime;
                    return time < 0 ? null : TimeSpan.FromSeconds(time).ToString(@"m\:ss");
                }
            }

            public Color Color
            {
                get
                {
                    switch (WardType)
                    {
                        case Type.BlueTrinket:
                            return Color.CornflowerBlue;
                        case Type.SightWard:
                            return Color.LimeGreen;
                        case Type.VisionWard:
                            return Color.DeepPink;
                        case Type.YellowTrinket:
                            return Color.Yellow;
                    }
                    return Color.Red;
                }
            }

            public Ward(AIHeroClient caster, Obj_AI_Base handle, Vector3 position, Type wardType, int duration, GameObjectTeam team)
            {
                // Initialize properties
                Caster = caster;
                Handle = handle;
                FakePosition = position;
                Team = team;
                WardType = wardType;
                Duration = duration * 1000;
                CreationTime = Core.GameTickCount;

                // Initialize rendering
                MinimapSprite = new EloBuddy.SDK.Rendering.Sprite(() => MinimapIconTexture);
                TextHandle = new Text(string.Empty, new System.Drawing.Font(FontFamily.GenericSansSerif, 8, FontStyle.Regular));

                // Notify player about placement
                if (!MasterMind.IsSpectatorMode && Team.IsEnemy() &&
                    (Player.Instance.IsInRange(Position, NotifyRange.CurrentValue) || Player.Instance.IsInRange(caster, NotifyRange.CurrentValue)))
                {
                    if (NotifyPlace.CurrentValue)
                    {
                        Notifications.Show(new SimpleNotification("A ward has been placed!", string.Format("{0} has placed a ward of type '{1}'", caster.ChampionName, WardType)));
                    }
                    if (NotifyPlacePing.CurrentValue)
                    {
                        TacticalMap.ShowPing(PingCategory.Normal, Position, true);
                    }
                }
            }

            public void RenderWard()
            {
                // TODO: Replace with fake object rendering once it's added by finn
                Circle.Draw(Color.ToBgra(150), Radius, 3, Position);
            }

            public void DrawMinimap()
            {
                // Draw the ward icon on the minimap
                var desc = MinimapSprite.Texture.GetLevelDescription(0);
                var offset = new Vector2(-desc.Width / 2f, -desc.Height / 2f);
                MinimapSprite.Draw(MinimapPosition + offset);
            }

            public void DrawTime()
            {
                // Apply the remaining time and draw it
                var time = RemainingTimeText;
                if (time != null)
                {
                    TextHandle.TextValue = "Ward: " + time;
                    TextHandle.Position = new Vector2(ScreenPosition.X - TextHandle.Bounding.Width / 2f, ScreenPosition.Y + HealthBar.Height);
                    TextHandle.Draw();
                }
            }

            public void DrawHealth()
            {
                // Get the screen position
                var pos = ScreenPosition;

                // Calculate the outline rectangle
                var rect = new RectangleF((int) (pos.X + HealthBar.X), (int) (pos.Y + HealthBar.Y), (int) HealthBar.Width, (int) HealthBar.Height);

                // Draw the background
                Drawing.DrawLine(new Vector2(rect.Left, rect.Top + rect.Height / 2), new Vector2(rect.Right, rect.Top + rect.Height / 2), rect.Height, HealthBarBackgroundColor);

                // Get the health percent
                var percent = (Handle == null ? MaxHealth : Handle.Health) / MaxHealth;

                // Draw the percent lines
                var colors = HealthBarColors[MasterMind.IsSpectatorMode ? Team : (Team.IsAlly() ? GameObjectTeam.Order : GameObjectTeam.Chaos)];
                var max = rect.Height - HealthBarBorderWidth * 2;
                for (var i = 0; i < max; i++)
                {
                    var start = new Vector2(rect.Left + HealthBarBorderWidth, rect.Top + HealthBarBorderWidth + i);
                    Line.DrawLine(Color.FromArgb((byte) (colors.Item1.R + (colors.Item2.R - colors.Item1.R) * (i / max)),
                        (byte) (colors.Item1.G + (colors.Item2.G - colors.Item1.G) * (i / max)),
                        (byte) (colors.Item1.B + (colors.Item2.B - colors.Item1.B) * (i / max))), 1, start, start + new Vector2((int) ((rect.Width - HealthBarBorderWidth * 2) * percent), 0));
                }

                // Draw the separating lines
                var step = 1 / MaxHealth;
                var currentStep = step;
                for (var i = 0; i < MaxHealth - 1; i++)
                {
                    // Draw the separator
                    var start = new Vector2((float) (rect.Left + HealthBarBorderWidth + Math.Round(currentStep * (rect.Width - HealthBarBorderWidth * 2))), rect.Top);
                    Drawing.DrawLine(start, start + new Vector2(0, rect.Height), HealthBarPadding, Color.FromArgb(200, HealthBarBackgroundColor));

                    // Increase step
                    currentStep += step;
                }
            }

            public static Ward ParseObject(AIHeroClient caster, Obj_AI_Base obj)
            {
                // Wards cannot have more than 5 max health
                if (obj.MaxHealth <= 5)
                {
                    // Validate base skin
                    if (!string.IsNullOrWhiteSpace(obj.BaseSkinName))
                    {
                        // Parse the ward type
                        Type wardType;
                        if (Enum.TryParse(obj.BaseSkinName, out wardType))
                        {
                            // Return the ward object
                            return new Ward(caster, obj, obj.Position, wardType, GetWardDuration(caster, wardType), caster.Team);
                        }
                    }
                }

                return null;
            }

            public static Ward FromSpellCast(AIHeroClient caster, Type wardType, Vector3 position)
            {
                return new Ward(caster, null, position.GetClosestWardSpot(), wardType, GetWardDuration(caster, wardType), caster.Team);
            }

            public static bool IsWard(Obj_AI_Base obj, out Type wardType)
            {
                // Wards cannot have more than 5 max health
                if (obj.MaxHealth <= 5)
                {
                    // Validate base skin
                    if (!string.IsNullOrWhiteSpace(obj.BaseSkinName))
                    {
                        // Parse the ward type
                        return Enum.TryParse(obj.BaseSkinName, out wardType);
                    }
                }

                wardType = 0;
                return false;
            }

            public static int GetWardDuration(AIHeroClient caster, Type wardType)
            {
                switch (wardType)
                {
                    case Type.SightWard:
                        return 150;
                    case Type.YellowTrinket:
                        return (int) Math.Ceiling(60 + 3.5 * (caster.Level - 1));
                }

                return -1;
            }
        }
    }

    public static partial class Extensions
    {
        public static ColorBGRA ToBgra(this Color color, byte alpha)
        {
            return new ColorBGRA(color.R, color.G, color.B, alpha);
        }

        public static ColorBGRA ToBgra(this Color color)
        {
            return new ColorBGRA(color.R, color.G, color.B, color.A);
        }

        public static Vector3 GetClosestWardSpot(this Vector3 wallPosition)
        {
            // If the position is not a wall then return the input position
            var flags = wallPosition.ToNavMeshCell().CollFlags;
            if (!flags.HasFlag(CollisionFlags.Wall) && !flags.HasFlag(CollisionFlags.Building))
            {
                return wallPosition;
            }

            const int maxRange = 10;
            const double step = 2 * Math.PI / 20;

            var start = new Vector2(wallPosition.ToNavMeshCell().GridX, wallPosition.ToNavMeshCell().GridY);
            var checkedCells = new HashSet<Vector2>();

            // Get all directions
            var directions = new List<Vector2>();
            for (var theta = 0d; theta <= 2 * Math.PI + step; theta += step)
            {
                directions.Add((new Vector2((short) (start.X + Math.Cos(theta)), (short) (start.Y - Math.Sin(theta))) - start).Normalized());
            }

            var validPositions = new HashSet<Vector3>();
            for (var range = 1; range < maxRange; range++)
            {
                foreach (var direction in directions)
                {
                    // Get the position to check
                    var end = start + range * direction;
                    var testPoint = new Vector2((short) end.X, (short) end.Y);
                    if (checkedCells.Contains(testPoint))
                    {
                        continue;
                    }
                    checkedCells.Add(testPoint);

                    // Check the position for wall flags
                    flags = new NavMeshCell((short) testPoint.X, (short) testPoint.Y).CollFlags;
                    if (!flags.HasFlag(CollisionFlags.Wall) && !flags.HasFlag(CollisionFlags.Building))
                    {
                        // Add the position to the valid positions set
                        validPositions.Add(NavMesh.GridToWorld((short) testPoint.X, (short) testPoint.Y));
                    }
                }

                if (validPositions.Count > 0)
                {
                    // Return the closest position to the initial wall position
                    return validPositions.OrderBy(o => o.Distance(start, true)).First();
                }
            }

            // Nothing found return input
            return wallPosition;
        }
    }
}
