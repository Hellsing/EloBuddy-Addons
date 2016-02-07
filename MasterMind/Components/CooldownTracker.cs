using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Runtime.Serialization;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;
using EloBuddy.SDK.Rendering;
using MasterMind.Properties;
using Newtonsoft.Json;
using SharpDX;
using SharpDX.Direct3D9;
using Color = System.Drawing.Color;
using Line = EloBuddy.SDK.Rendering.Line;

namespace MasterMind.Components
{
    public sealed class CooldownTracker : IComponent
    {
        #region Offsets

        public static readonly Vector2 OverlayOffset = new Vector2(1, 19);
        public static readonly Vector2 OverlaySummonerOffset = OverlayOffset + new Vector2(106, 4);

        public static readonly Vector2 SpellOffset = OverlayOffset + new Vector2(30, 5);
        public static readonly Vector2 SpellSize = new Vector2(18, 5);
        public const int SpellLinePadding = 1;
        public static readonly int SpellCooldownTextOffset = (int) SpellSize.Y;

        private static readonly Vector2 SummonerOffset = OverlaySummonerOffset + new Vector2(3, 5);
        private static readonly Vector2 SummonerSize = new Vector2(10);
        public const int SummonerPadding = 0;
        public static readonly int SummonerCooldownTextOffset = (int) SummonerSize.Y + 3;
        public static readonly int SummonerCooldownTextPadding = 4;
        public static readonly Vector3 CooldownCenter = new Vector3(SummonerSize.X / 2, SummonerSize.Y / 2, 0);

        public static SummonerAtlas SummonerAtlas { get; private set; }

        #endregion

        public static readonly Color SpellBackground = Color.SlateGray;
        public static readonly Color SpellNotLearned = Color.DimGray;
        public static readonly Color SpellReady = Color.LawnGreen;
        public static readonly Color SpellNotReady = Color.Red;

        #region Textures

        private static string _spellOverlayRef;
        private static Texture SpellOverlayTexture
        {
            get { return MasterMind.TextureLoader[_spellOverlayRef]; }
        }

        private static string _summonerOverlayRef;
        private static Texture SummonerOverlayTexture
        {
            get { return MasterMind.TextureLoader[_summonerOverlayRef]; }
        }

        private static string _summonersRef;
        private static Texture SummonersTexture
        {
            get { return MasterMind.TextureLoader[_summonersRef]; }
        }

        private static Bitmap SummonerSpells
        {
            get
            {
                var result = new Bitmap(100, 40);
                using (var g = Graphics.FromImage(result))
                {
                    g.SmoothingMode = SmoothingMode.HighQuality;
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    g.DrawImage(Resources.Summoners, 0, 0, result.Width, result.Height);
                }

                return result;
            }
        }

        #endregion

        #region Rendering

        private EloBuddy.SDK.Rendering.Sprite SpellOverlaySprite { get; set; }
        private EloBuddy.SDK.Rendering.Sprite SummonerOverlaySprite { get; set; }
        private EloBuddy.SDK.Rendering.Sprite SummonersSprite { get; set; }
        private Text CooldownText { get; set; }

        #endregion

        #region Menu

        public Menu Menu { get; private set; }

        public CheckBox TrackAllies { get; private set; }
        public CheckBox TrackEnemies { get; private set; }
        public CheckBox DrawSummoners { get; private set; }
        public CheckBox DrawText { get; private set; }

        #endregion

        public GameObjectTeam AlliedTeam { get; set; }

        public bool ShouldLoad(bool isSpectatorMode = false)
        {
            // Always load, regardless the game mode
            return true;
        }

        public void InitializeComponent()
        {
            // Initialize texture references
            MasterMind.TextureLoader.Load(Resources.MainFrame, out _spellOverlayRef);
            MasterMind.TextureLoader.Load(Resources.SummonerSlots, out _summonerOverlayRef);
            MasterMind.TextureLoader.Load(SummonerSpells, out _summonersRef);
            SummonerAtlas = JsonConvert.DeserializeObject<SummonerAtlas>(Resources.SummonerAtlas);

            // Initialize menu
            Menu = MasterMind.Menu.AddSubMenu("Cooldown Tracker", longTitle: "Spell Cooldown Tracker");

            Menu.AddGroupLabel("Information");
            Menu.AddLabel("A spell cooldown tracker helps you in various ways ingame.");
            Menu.AddLabel("It lets you visually see the remaining time the spells are on cooldown.");
            Menu.AddLabel(string.Format("You can enable cooldown tracking for both, {0} and {1}.",
                MasterMind.IsSpectatorMode ? "blue" : "allies", MasterMind.IsSpectatorMode ? "red team" : "enemies"));
            Menu.AddSeparator();

            TrackAllies = Menu.Add("allies", new CheckBox(string.Format("Track {0}", MasterMind.IsSpectatorMode ? "blue team" : "allies")));
            TrackEnemies = Menu.Add("enemies", new CheckBox(string.Format("Track {0}", MasterMind.IsSpectatorMode ? "red team" : "enemies")));
            DrawSummoners = Menu.Add("summoners", new CheckBox("Draw summoner spells"));
            DrawText = Menu.Add("cooldownText", new CheckBox("Draw cooldown time below spell indicator"));

            // Initialize properties
            SpellOverlaySprite = new EloBuddy.SDK.Rendering.Sprite(() => SpellOverlayTexture);
            SummonerOverlaySprite = new EloBuddy.SDK.Rendering.Sprite(() => SummonerOverlayTexture);
            SummonersSprite = new EloBuddy.SDK.Rendering.Sprite(SummonersTexture);
            CooldownText = new Text(string.Empty, new System.Drawing.Font(FontFamily.GenericSansSerif, 8, FontStyle.Regular))
            {
                Color = Color.GhostWhite
            };

            AlliedTeam = GameObjectTeam.Order;
            if (!MasterMind.IsSpectatorMode)
            {
                AlliedTeam = Player.Instance.Team;
            }

            // Listen to required events
            Drawing.OnEndScene += OnDraw;
        }

        private void OnDraw(EventArgs args)
        {
            foreach (var hero in EntityManager.Heroes.AllHeroes.Where(o => (MasterMind.IsSpectatorMode || !o.IsMe) && o.IsHPBarRendered && o.IsVisible))
            {
                // Validate team
                if (hero.Team == AlliedTeam)
                {
                    if (!TrackAllies.CurrentValue)
                    {
                        continue;
                    }
                }
                else
                {
                    if (!TrackEnemies.CurrentValue)
                    {
                        continue;
                    }
                }

                // Get the HP bar position
                var pos = hero.HPBarPosition.Round();

                // Draw the spell lines and numbers
                for (var i = 0; i < 4; i ++)
                {
                    // Get the spell
                    var spell = hero.Spellbook.GetSpell((SpellSlot) i);

                    // Start position of the line
                    var start = pos + SpellOffset + new Vector2(i * (SpellSize.X + SpellLinePadding), 0);

                    // Get the current cooldown
                    var cooldown = Math.Max(0, spell.CooldownExpires - Game.Time);

                    // Check if the spell is ready
                    if (spell.IsLearned && cooldown <= 0)
                    {
                        Line.DrawLine(SpellReady, SpellSize.Y, start, start + new Vector2(SpellSize.X, 0));
                    }
                    else
                    {
                        // Calculate percent cooldown
                        var percent = 1 - Math.Min(1, cooldown / spell.Cooldown);

                        // Calculate end position
                        var end = start + new Vector2(SpellSize.X * percent, 0);

                        // Draw the lines
                        Line.DrawLine(spell.IsLearned ? SpellBackground : SpellNotLearned, SpellSize.Y, start, start + new Vector2(SpellSize.X, 0));
                        if (spell.IsLearned)
                        {
                            Line.DrawLine(spell.GetSpellColor(percent), SpellSize.Y, start, end);

                            // Draw the remaining time as text
                            if (DrawText.CurrentValue)
                            {
                                CooldownText.TextValue = ((int) Math.Ceiling(cooldown)).ToString();
                                CooldownText.Position = new Vector2(start.X + SpellSize.X / 2 - CooldownText.Bounding.Width / 2f, start.Y + SpellCooldownTextOffset);
                                CooldownText.Draw();
                            }
                        }
                    }
                }

                // Draw the overlay
                SpellOverlaySprite.Draw(pos + OverlayOffset);

                if (DrawSummoners.CurrentValue)
                {
                    for (var i = 0; i < 2; i++)
                    {
                        // Get the spell
                        var spell = hero.Spellbook.GetSpell(i + SpellSlot.Summoner1);

                        // Start position of the line
                        var start = pos + SummonerOffset + new Vector2(i * (SummonerSize.X + SummonerPadding), 0);

                        // Get the current cooldown
                        var cooldown = Math.Max(0, spell.CooldownExpires - Game.Time);

                        // Draw the summoner spell image
                        var summoner = SummonerAtlas[spell.Name];
                        if (summoner != null)
                        {
                            if (cooldown <= 0)
                            {
                                // Draw the regular summoner image
                                SummonersSprite.Draw(start, summoner.Rectangle);
                            }
                            else
                            {
                                // Calculate percent cooldown
                                var percent = 1 - Math.Min(1, cooldown / spell.Cooldown);

                                // Calculate radian
                                var radian = (float) (2 * Math.PI * percent);

                                if (percent < 0.5f)
                                {
                                    // Draw right side of the summoner image
                                    SummonersSprite.Draw(start + new Vector2((int) (SummonerAtlas.SpriteAtlas.Width / 2f), 0), summoner.RightHalf);

                                    // Draw rotated cooldown circle
                                    SummonersSprite.Draw(start, SummonerAtlas.GetCooldownRectangle(SummonerAtlas.CooldownType.Circle), CooldownCenter, radian);

                                    // Draw left side of the summoner image
                                    SummonersSprite.Draw(start, summoner.LeftHalf);

                                    // Draw cooldown overlay
                                    SummonersSprite.Draw(start, SummonerAtlas.GetCooldownRectangle(SummonerAtlas.CooldownType.Overlay));
                                }
                                else
                                {
                                    // Draw left side of the summoner image
                                    SummonersSprite.Draw(start, summoner.LeftHalf);

                                    // Draw rotated cooldown circle
                                    SummonersSprite.Draw(start, SummonerAtlas.GetCooldownRectangle(SummonerAtlas.CooldownType.Circle), CooldownCenter, radian);

                                    // Draw right side of the summoner image
                                    SummonersSprite.Draw(start + new Vector2((int) (SummonerAtlas.SpriteAtlas.Width / 2f), 0), summoner.RightHalf);
                                }

                                // Draw the cooldown border
                                SummonersSprite.Draw(start, SummonerAtlas.GetCooldownRectangle(SummonerAtlas.CooldownType.Border));
                            }
                        }

                        // Draw the remaining time as text
                        if (DrawText.CurrentValue && cooldown > 0)
                        {
                            var text = TimeSpan.FromSeconds((int) Math.Ceiling(cooldown)).ToString("ss");
                            if (cooldown > 59)
                            {
                                text = TimeSpan.FromSeconds((int) Math.Ceiling(cooldown)).Minutes.ToString();
                            }
                            CooldownText.TextValue = text;
                            CooldownText.Position = new Vector2(start.X + SummonerSize.X / 2 - CooldownText.Bounding.Width / 2f + ((SummonerCooldownTextPadding / 2) * (i % 2 == 0 ? -1 : 1)),
                                start.Y + SummonerCooldownTextOffset);
                            CooldownText.Draw();
                        }
                    }

                    // Draw the overlay
                    SummonerOverlaySprite.Draw(pos + OverlaySummonerOffset);
                }
            }
        }
    }

    [DataContract]
    public class SummonerAtlas
    {
        public enum CooldownType
        {
            Overlay,
            Circle,
            Border
        }

        [DataMember]
        public SpriteAtlas SpriteAtlas { get; private set; }

        public SummonerOffset this[string summonerName]
        {
            get
            {
                var lowerName = summonerName.ToLower();
                return SpriteAtlas.Summoners.Where(entry => lowerName.Contains(entry.Key.ToLower()) ||
                                                            (entry.Value.AlternativeNames != null
                                                             && entry.Value.AlternativeNames.Any(o => lowerName.Contains(o.ToLower())))).Select(entry => entry.Value).FirstOrDefault();
            }
        }

        public SharpDX.Rectangle GetCooldownRectangle(CooldownType type)
        {
            return SpriteAtlas.Cooldowns[type.ToString()].Rectangle;
        }
    }

    [DataContract]
    public class SpriteAtlas
    {
        [DataMember(IsRequired = true)]
        public int Width { get; private set; }
        [DataMember(IsRequired = true)]
        public int Height { get; private set; }
        [DataMember(IsRequired = true)]
        public Dictionary<string, SummonerOffset> Summoners { get; private set; }
        [DataMember(IsRequired = true)]
        public Dictionary<string, CooldownOffset> Cooldowns { get; private set; }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            // Initialize rectangles
            foreach (var summoner in Summoners.Values)
            {
                summoner.Rectangle = new SharpDX.Rectangle(summoner.X, summoner.Y, Width, Height);
                summoner.LeftHalf = new SharpDX.Rectangle(summoner.X, summoner.Y, Width / 2, Height);
                summoner.RightHalf = new SharpDX.Rectangle(summoner.X + Width / 2, summoner.Y, Width / 2, Height);
            }
            foreach (var summoner in Cooldowns.Values)
            {
                summoner.Rectangle = new SharpDX.Rectangle(summoner.X, summoner.Y, Width, Height);
            }
        }
    }

    [DataContract]
    public class SummonerOffset
    {
        [DataMember(IsRequired = true)]
        public int X { get; private set; }
        [DataMember(IsRequired = true)]
        public int Y { get; private set; }

        [DataMember]
        public List<string> AlternativeNames { get; private set; }

        public SharpDX.Rectangle Rectangle { get; set; }
        public SharpDX.Rectangle LeftHalf { get; set; }
        public SharpDX.Rectangle RightHalf { get; set; }
    }

    [DataContract]
    public class CooldownOffset
    {
        [DataMember(IsRequired = true)]
        public int X { get; private set; }
        [DataMember(IsRequired = true)]
        public int Y { get; private set; }

        public SharpDX.Rectangle Rectangle { get; set; }
    }

    public static partial class Extensions
    {
        public static Vector2 Round(this Vector2 vector)
        {
            return new Vector2((int) Math.Round(vector.X), (int) Math.Round(vector.Y));
        }

        public static Color GetSpellColor(this SpellDataInst spellData, float percent = 0)
        {
            // Calculate new color between the ready and not ready colors based on the percent
            return Color.FromArgb((byte) (CooldownTracker.SpellNotReady.R + (CooldownTracker.SpellReady.R - CooldownTracker.SpellNotReady.R) * percent),
                (byte) (CooldownTracker.SpellNotReady.G + (CooldownTracker.SpellReady.G - CooldownTracker.SpellNotReady.G) * percent),
                (byte) (CooldownTracker.SpellNotReady.B + (CooldownTracker.SpellReady.B - CooldownTracker.SpellNotReady.B) * percent));
        }
    }
}
