using System;
using System.Drawing;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;
using EloBuddy.SDK.Rendering;
using MasterMind.Properties;
using SharpDX;
using SharpDX.Direct3D9;
using Color = System.Drawing.Color;
using Line = EloBuddy.SDK.Rendering.Line;

namespace MasterMind.Components
{
    public sealed class CooldownTracker : IComponent
    {
        public static readonly Vector2 OverlayOffset = new Vector2(1, 19);

        public static readonly Vector2 SpellOffset = OverlayOffset + new Vector2(30, 5);
        public static readonly Vector2 SpellSize = new Vector2(18, 5);
        public const int SpellLinePadding = 1;
        public static readonly int CooldownTextOffset = (int) SpellSize.Y;

        public static readonly Color SpellBackground = Color.SlateGray;
        public static readonly Color SpellNotLearned = Color.DimGray;
        public static readonly Color SpellReady = Color.LawnGreen;
        public static readonly Color SpellNotReady = Color.Red;

        private static string _overlayRef;
        private static Texture OverlayTextre
        {
            get { return MasterMind.TextureLoader[_overlayRef]; }
        }

        private EloBuddy.SDK.Rendering.Sprite OverlaySprite { get; set; }
        private Text SpellCooldownText { get; set; }

        public Menu Menu { get; private set; }

        public CheckBox TrackAllies { get; private set; }
        public CheckBox TrackEnemies { get; private set; }
        public CheckBox DrawText { get; private set; }

        public GameObjectTeam AlliedTeam { get; set; }

        public bool ShouldLoad(bool isSpectatorMode = false)
        {
            // Always load, regardless the game mode
            return true;
        }

        public void InitializeComponent()
        {
            // Initialize texture references
            MasterMind.TextureLoader.Load(Resources.CooldownTracker, out _overlayRef);

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
            DrawText = Menu.Add("cooldownText", new CheckBox("Draw cooldown time below spell indicator"));

            // Initialize properties
            OverlaySprite = new EloBuddy.SDK.Rendering.Sprite(() => OverlayTextre);
            SpellCooldownText = new Text(string.Empty, new System.Drawing.Font(FontFamily.GenericSansSerif, 8, FontStyle.Regular))
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
                                SpellCooldownText.TextValue = ((int) Math.Ceiling(cooldown)).ToString();
                                SpellCooldownText.Position = new Vector2(start.X + SpellSize.X / 2 - SpellCooldownText.Bounding.Width / 2f, start.Y + CooldownTextOffset);
                                SpellCooldownText.Draw();
                            }
                        }
                    }
                }

                // Draw the overlay
                OverlaySprite.Draw(pos + OverlayOffset);
            }
        }
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
