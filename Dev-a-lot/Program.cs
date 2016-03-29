using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using EloBuddy;
using EloBuddy.Sandbox;
using EloBuddy.SDK;
using EloBuddy.SDK.Events;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;
using EloBuddy.SDK.Rendering;
using SharpDX;
using Color = System.Drawing.Color;

namespace DevALot
{
    internal class Program
    {
        public static readonly string ResultPath = Path.Combine(SandboxConfig.DataDirectory, "Dev-a-lot");

        private const string BuffsFormatNormal = "DisplayName: {0} | Caster: {1} | Count: {2}";
        private const string BuffsFormatAdvanced = "DisplayName: {0} | Name: {1} | Caster: {2} | SourceName: {3} | Count: {4} | RemainingTime: {5}";

        public static Menu Menu { get; set; }

        #region Menu Values

        private static bool ShowGeneral
        {
            get { return Menu["general"].Cast<CheckBox>().CurrentValue; }
        }
        private static bool ShowBuffs
        {
            get { return Menu["buffs"].Cast<CheckBox>().CurrentValue; }
        }
        private static bool ShowBuffsAdv
        {
            get { return Menu["buffs+"].Cast<CheckBox>().CurrentValue; }
        }
        private static bool HeroesOnly
        {
            get { return Menu["heroes"].Cast<CheckBox>().CurrentValue; }
        }
        private static bool ShowAaDamage
        {
            get { return Menu["autoAttack"].Cast<CheckBox>().CurrentValue; }
        }
        private static bool AnalyzeAzir
        {
            get { return Menu["azir"].Cast<CheckBox>().CurrentValue; }
        }

        private static bool ObjectNames
        {
            get { return Menu["objectNames"].Cast<CheckBox>().CurrentValue; }
        }
        private static bool ShowMouse
        {
            get { return Menu["mouse"].Cast<CheckBox>().CurrentValue; }
        }
        private static bool ShowMouseLines
        {
            get { return Menu["mouseLines"].Cast<CheckBox>().CurrentValue; }
        }
        private static bool ShowGrid
        {
            get { return Menu["grid"].Cast<CheckBox>().CurrentValue; }
        }
        private static int GridSize
        {
            get { return Menu["gridSize"].Cast<Slider>().CurrentValue; }
        }

        #endregion

        private static Vector2 CurrentGridPosition { get; set; }
        private static int CurrentGridSize { get; set; }
        private static Dictionary<short, Dictionary<short, float>> GridHeight { get; set; }

        private static void Main(string[] args)
        {
            Loading.OnLoadingComplete += OnLoadingComplete;
            Loading.OnLoadingCompleteSpectatorMode += OnLoadingComplete;
        }

        private static void OnLoadingComplete(EventArgs args)
        {
            #region Menu Creation

            // Setup a menu
            Menu = MainMenu.AddMenu("Dev-a-lot", "devalot");

            Menu.AddGroupLabel("General GameObject analyzing");
            Menu.Add("general", new CheckBox("General properties", false)).CurrentValue = false;
            Menu.Add("heroes", new CheckBox("Heroes only"));
            Menu.Add("buffs", new CheckBox("Show buffs", false)).CurrentValue = false;
            Menu.Add("buffs+", new CheckBox("Show more buff info", false));
            if (!Bootstrap.IsSpectatorMode)
            {
                Menu.Add("autoAttack", new CheckBox("Show auto attack damage", false)).CurrentValue = false;
                if (Player.Instance.Hero == Champion.Azir)
                {
                    Menu.Add("azir", new CheckBox("Analyze Azir soldiers", false));
                }
            }

            Menu.AddGroupLabel("Near mouse analyzing");
            Menu.Add("objectNames", new CheckBox("General info about object", false));
            Menu.Add("mouse", new CheckBox("Show info about mouse position", false));
            Menu.Add("mouseLines", new CheckBox("Show mouse coordinate lines", false));
            Menu.Add("grid", new CheckBox("Visualize game grid", false));
            Menu.Add("gridSize", new Slider("Grid size {0} x {0}", 11, 1, 55)).OnValueChange += delegate { OnMouseMove(null); };

            #endregion

            // Initialize other things
            EventVerifier.Initialize();
            PropertyVerifier.Initialize();
            SDKVerifier.Initialize();

            // Listen to all required events
            Messages.RegisterEventHandler<Messages.MouseMove>(OnMouseMove);
            Drawing.OnDraw += OnDraw;
        }

        private static void OnDraw(EventArgs args)
        {
            const float analyzeRange = 500;

            #region Visualize Game Grid

            if (ShowGrid)
            {
                var sourceGrid = Game.CursorPos.ToNavMeshCell();
                var startPos = new NavMeshCell(sourceGrid.GridX - (short) Math.Floor(GridSize / 2f), sourceGrid.GridY - (short) Math.Floor(GridSize / 2f));

                var cells = new List<NavMeshCell> { startPos };
                for (var y = startPos.GridY; y < startPos.GridY + GridSize; y++)
                {
                    for (var x = startPos.GridX; x < startPos.GridX + GridSize; x++)
                    {
                        if (x == startPos.GridX && y == startPos.GridY)
                        {
                            continue;
                        }
                        if (x == sourceGrid.GridX && y == sourceGrid.GridY)
                        {
                            cells.Add(sourceGrid);
                        }
                        else
                        {
                            cells.Add(new NavMeshCell(x, y));
                        }
                    }
                }

                foreach (var cell in cells.OrderBy(o => o.CollFlags))
                {
                    var color = Color.AntiqueWhite;
                    if (cell.CollFlags.HasFlag(CollisionFlags.Wall))
                    {
                        color = Color.DodgerBlue;
                    }
                    else if (cell.CollFlags.HasFlag(CollisionFlags.Grass))
                    {
                        color = Color.LimeGreen;
                    }
                    else if (cell.CollFlags.HasFlag((CollisionFlags) 256))
                    {
                        color = Color.Yellow;
                    }

                    var world2D = cell.WorldPosition.To2D();

                    Line.DrawLine(color,
                        cell.WorldPosition,
                        (world2D + new Vector2(NavMesh.CellWidth, 0)).To3DWorld(),
                        (world2D + new Vector2(NavMesh.CellWidth, NavMesh.CellHeight)).To3DWorld(),
                        (world2D + new Vector2(0, NavMesh.CellHeight)).To3DWorld(),
                        cell.WorldPosition);
                }
            }

            #endregion

            #region Mouse Analyzing

            if (ShowMouseLines)
            {
                Line.DrawLine(Color.GhostWhite, new Vector2(Game.CursorPos2D.X, 0), new Vector2(Game.CursorPos2D.X, Drawing.Height));
                Line.DrawLine(Color.GhostWhite, new Vector2(0, Game.CursorPos2D.Y), new Vector2(Drawing.Width, Game.CursorPos2D.Y));
            }

            if (ShowMouse)
            {
                Drawing.DrawText(Game.CursorPos2D + new Vector2(40, 0), Color.Orange, string.Format("Screen Position: X:{0} Y:{1}", Game.CursorPos2D.X, Game.CursorPos2D.Y), 10);
                Drawing.DrawText(Game.CursorPos2D + new Vector2(40, 20), Color.Orange, string.Format("Game Position: X:{0} Y:{1} Z:{2}",
                    Math.Round(Game.CursorPos.X), Math.Round(Game.CursorPos.Y), Math.Round(Game.CursorPos.Z)), 10);
                var navMeshCell = Game.CursorPos.ToNavMeshCell();
                Drawing.DrawText(Game.CursorPos2D + new Vector2(40, 40), Color.Orange, string.Format("NavMesh Position: X:{0} Y:{1}",
                    navMeshCell.GridX, navMeshCell.GridY), 10);

                Drawing.DrawText(Game.CursorPos2D + new Vector2(40, 60), Color.NavajoWhite, string.Format("Collision flags: {0}", navMeshCell.CollFlags), 10);
            }

            if (ObjectNames)
            {
                Circle.Draw(SharpDX.Color.Red, analyzeRange, Game.CursorPos);
            }

            #endregion

            #region Object Analyzing

            foreach (var obj in (HeroesOnly ? ObjectManager.Get<AIHeroClient>() : ObjectManager.Get<GameObject>()).Where(o => o.VisibleOnScreen))
            {
                var i = 0;
                const int step = 20;

                var baseObject = obj as Obj_AI_Base;

                #region Near Mouse Analyzing

                if (ObjectNames && obj.IsInRange(Game.CursorPos, analyzeRange))
                {
                    Drawing.DrawText(obj.Position.WorldToScreen() + new Vector2(0, i), Color.Orange, "General info", 10);
                    i += step;

                    var data = new Dictionary<string, object>
                    {
                        { "System.Type", obj.GetType().Name },
                        { "GameObjectType", obj.Type },
                        { "Name", obj.Name },
                        { "Position", obj.Position }
                    };
                    foreach (var dataEntry in data)
                    {
                        Drawing.DrawText(obj.Position.WorldToScreen() + new Vector2(0, i), Color.NavajoWhite, string.Format("{0}: {1}", dataEntry.Key, dataEntry.Value), 10);
                        i += step;
                    }
                    Circle.Draw(SharpDX.Color.DarkRed, obj.BoundingRadius, obj.Position);
                }

                #endregion

                #region General Properties

                if (ShowGeneral && baseObject != null)
                {
                    var data = new Dictionary<string, object>
                    {
                        { "BaseSkinName", baseObject.BaseSkinName },
                        { "Model", baseObject.Model },
                        { "Health", baseObject.Health },
                        { "Mana", baseObject.Mana },
                        { "BoundingRadius", baseObject.BoundingRadius },
                        { "IsValid", baseObject.IsValid },
                        { "IsVisible", baseObject.IsVisible },
                        { "IsTargetable", baseObject.IsTargetable },
                        { "IsDead", baseObject.IsDead },
                        { "IsHPBarRendered", baseObject.IsHPBarRendered }
                    };

                    Drawing.DrawText(baseObject.Position.WorldToScreen() + new Vector2(0, i), Color.Orange, "General properties", 10);
                    i += step;
                    foreach (var dataEntry in data)
                    {
                        Drawing.DrawText(baseObject.Position.WorldToScreen() + new Vector2(0, i), Color.NavajoWhite, string.Format("{0}: {1}", dataEntry.Key, dataEntry.Value), 10);
                        i += step;
                    }
                }

                #endregion

                #region Buffs

                if (ShowBuffs && baseObject != null)
                {
                    Drawing.DrawText(baseObject.Position.WorldToScreen() + new Vector2(0, i), Color.Orange, "Buffs", 10);
                    i += step;
                    foreach (var buff in baseObject.Buffs.Where(o => o.IsValid()))
                    {
                        if (ShowBuffsAdv)
                        {
                            var endTime = Math.Max(0, buff.EndTime - Game.Time);
                            Drawing.DrawText(baseObject.Position.WorldToScreen() + new Vector2(0, i), Color.NavajoWhite,
                                string.Format(BuffsFormatAdvanced, buff.DisplayName, buff.Name, buff.Caster.Name, buff.SourceName, buff.Count,
                                    endTime > 1000 ? "Infinite" : Convert.ToString(endTime, CultureInfo.InvariantCulture), buff.Name), 10);
                        }
                        else
                        {
                            Drawing.DrawText(baseObject.Position.WorldToScreen() + new Vector2(0, i), Color.NavajoWhite,
                                string.Format(BuffsFormatNormal, buff.DisplayName, buff.Caster.Name, buff.Count), 10);
                        }
                        i += step;
                    }
                }

                #endregion

                #region Auto Attack Damage

                if (!Bootstrap.IsSpectatorMode)
                {
                    if (ShowAaDamage && baseObject != null && baseObject.IsTargetableToTeam && !baseObject.IsAlly)
                    {
                        var damageWithPassive = Player.Instance.GetAutoAttackDamage(baseObject, true);
                        var damageWithoutPassive = Player.Instance.GetAutoAttackDamage(baseObject);
                        var difference = Math.Round(damageWithPassive - damageWithoutPassive);
                        Drawing.DrawText(baseObject.HPBarPosition, Color.NavajoWhite, string.Format("Damage: {0} ({1})", damageWithPassive, string.Concat(difference > 0 ? "+" : "", difference)),
                            10);
                    }
                }

                #endregion

                #region Azir Soldiers

                if (!Bootstrap.IsSpectatorMode)
                {
                    if (Player.Instance.Hero == Champion.Azir && AnalyzeAzir)
                    {
                        foreach (var soldier in Orbwalker.AzirSoldiers)
                        {
                            Circle.Draw(SharpDX.Color.DarkRed, soldier.BoundingRadius, soldier.Position);
                            Drawing.DrawText(soldier.Position.WorldToScreen(), Color.NavajoWhite, string.Format("Type: {0} | Name: {1}", soldier.GetType().Name, soldier.Name), 10);

                            Drawing.DrawText(soldier.Position.WorldToScreen() + new Vector2(0, 20), Color.NavajoWhite,
                                string.Format("Buffs: {0}", string.Join(" | ", soldier.Buffs.Select(o => string.Format("{0} ({1}x - {2})", o.DisplayName, o.Count, o.SourceName)))), 10);

                            Circle.Draw(SharpDX.Color.LawnGreen, 275, soldier.Position);
                            Drawing.DrawLine(Player.Instance.Position.WorldToScreen(), Player.Instance.Position.Extend(soldier, Player.Instance.AttackRange).To3DWorld().WorldToScreen(), 3,
                                Color.OrangeRed);

                            if (Orbwalker.ValidAzirSoldiers.Any(o => o.IdEquals(soldier)))
                            {
                                Circle.Draw(SharpDX.Color.AliceBlue, 500, soldier.Position);

                                foreach (var enemy in EntityManager.MinionsAndMonsters.Combined.Where(unit => unit.Team != Player.Instance.Team && unit.IsValidTarget()).Cast<Obj_AI_Base>()
                                    .Concat(EntityManager.Heroes.Enemies.Where(o => o.IsValidTarget())).Where(enemy => enemy.IsInRange(soldier, 275 + enemy.BoundingRadius)))
                                {
                                    Circle.Draw(SharpDX.Color.Red, enemy.BoundingRadius, enemy.Position);
                                }
                            }
                        }
                    }
                }

                #endregion
            }

            #endregion
        }

        private static void OnMouseMove(Messages.MouseMove args)
        {
            if (!ShowGrid)
            {
                return;
            }

            CalculateGridHeight();

            if (CurrentGridSize != GridSize)
            {
                // Recalculate grid size
            }
            var grid = Game.CursorPos.ToNavMeshCell();
            if ((short) CurrentGridPosition.X != grid.GridX || (short) CurrentGridPosition.Y != grid.GridY)
            {
                // Recalculate grid position
                CurrentGridPosition = new Vector2(grid.GridX, grid.GridY);
            }
        }

        private static void CalculateGridHeight()
        {
            if (GridHeight == null)
            {
                GridHeight = new Dictionary<short, Dictionary<short, float>>();

                for (float x = 0; x <= NavMesh.Width; x += NavMesh.CellWidth)
                {
                    GridHeight.Add((short) x, new Dictionary<short, float>());

                    for (float y = 0; y <= NavMesh.Height; y += NavMesh.CellHeight)
                    {
                        var pos = new Vector2(x, y).GridToWorld();
                        GridHeight[(short) x].Add((short) y, NavMesh.GetHeightForPosition(pos.X, pos.Y));
                    }
                }
            }
        }
    }
}
