using System;
using System.Drawing;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Enumerations;
using EloBuddy.SDK.Events;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;
using EloBuddy.SDK.Rendering;

namespace Karthus
{
    public sealed class Karthus
    {
        private static Karthus _instance;
        public static Karthus Instance
        {
            get { return _instance ?? (_instance = new Karthus()); }
        }

        internal static void Main(string[] args)
        {
            // Wait till the game has fully loaded
            Loading.OnLoadingComplete += OnLoadingComplete;
        }

        private static void OnLoadingComplete(EventArgs args)
        {
            // Check for the correct champion
            if (Player.Instance.Hero != Champion.Karthus)
            {
                return;
            }

            // Initialize the addons
            Instance.Initialize();
        }

        public Menu Menu { get; private set; }
        public Menu DrawingMenu { get; private set; }
        public SpellHandler SpellHandler { get; private set; }
        public ModeHandler ModeHandler { get; private set; }
        public UltimateAlerter UltimateAlerter { get; private set; }
        public bool Initialized { get; private set; }

        public bool IsDead
        {
            get { return Player.Instance.Buffs.Any(o => o.Name == "KarthusDeathDefiedBuff"); }
        }

        private readonly HitChance[] _hitchances =
        {
            HitChance.Low,
            HitChance.AveragePoint,
            HitChance.Medium,
            HitChance.High
        };

        private Karthus()
        {
            // Initialize properties
            Menu = MainMenu.AddMenu("Karthus 3K", "karthus", "Karthus - King Killsteal");
            SpellHandler = new SpellHandler(this,
                new Spell.Skillshot(SpellSlot.Q, 875, SkillShotType.Circular, spellSpeed: int.MaxValue, spellWidth: 160 * 2, castDelay: 750),
                new Spell.Skillshot(SpellSlot.W, 1000, SkillShotType.Circular, spellWidth: 100),
                new Spell.Active(SpellSlot.E, 550),
                new Spell.Active(SpellSlot.R));

            #region Setup Global Menu

            Menu.AddGroupLabel("Welcome Karthus 3K!");
            Menu.AddLabel("You can configure the addon on the left by navigating through the menu entries.");
            Menu.AddLabel("Below you can find a list of global configurations.");

            Menu.AddSeparator();
            Menu.AddGroupLabel("Global configurations");
            Menu.Add("ComboWhileDead", new CheckBox("Combo while dead"));

            Menu.AddSeparator();
            Menu.AddGroupLabel("Hitchances for spells");
            Menu.AddLabel("Here you can define your desired minimum hitchances for each spell. Default is Medium.");
            RegisterHitchances(Menu);

            #endregion

            // Setup mode handler
            ModeHandler = new ModeHandler(this);

            #region Setup Drawing Menu

            DrawingMenu = Menu.AddSubMenu("Drawings");
            DrawingMenu.AddGroupLabel("Info");
            DrawingMenu.AddLabel("You can enable and disable spell range drawings in here.");

            DrawingMenu.AddSeparator();
            DrawingMenu.AddGroupLabel("Spell ranges");
            DrawingMenu.Add("Q", new CheckBox("Draw Q range"));
            DrawingMenu.Add("E", new CheckBox("Draw E range", false));
            DrawingMenu.Add("W", new CheckBox("Draw W range"));
            DrawingMenu.Add("W2", new CheckBox("Draw W max range"));

            DrawingMenu.AddSeparator();
            DrawingMenu.AddGroupLabel("Ultimate (R) information");
            DrawingMenu.Add("showUltimate", new CheckBox("Display killable info near mouse"));

            #endregion

            // Setup damage indicator
            DamageIndicator.Initialize(target => Damages.GetTotalDamage(this, target));
            DamageIndicator.DrawingColor = Color.Goldenrod;

            // Setup ultimate alerter
            UltimateAlerter = new UltimateAlerter(this);

            // Listen to required events
            Game.OnTick += OnTick;
            Drawing.OnDraw += OnDraw;
        }

        private void RegisterHitchances(Menu menu)
        {
            for (var i = 0; i < 4; i++)
            {
                Spell.SpellBase spellBase = null;
                var slot = (SpellSlot) i;
                switch (slot)
                {
                    case SpellSlot.Q:
                        spellBase = SpellHandler.Q;
                        break;
                    case SpellSlot.W:
                        spellBase = SpellHandler.W;
                        break;
                    case SpellSlot.E:
                        spellBase = SpellHandler.E;
                        break;
                    case SpellSlot.R:
                        spellBase = SpellHandler.R;
                        break;
                }

                Spell.Skillshot skillshot;
                if ((skillshot = spellBase as Spell.Skillshot) != null)
                {
                    var spellEntry = new ComboBox(skillshot.Slot + " hitchance", _hitchances.Select(o => o.ToString()), 2);
                    spellEntry.OnValueChange += delegate(ValueBase<int> sender, ValueBase<int>.ValueChangeArgs args) { skillshot.MinimumHitChance = _hitchances[args.NewValue]; };
                    menu.Add("hitchance" + skillshot.Slot, spellEntry);
                }
            }
        }

        private void OnDraw(EventArgs args)
        {
            #region Spell Ranges

            if (SpellHandler.Q.IsLearned && DrawingMenu.Get<CheckBox>("Q").CurrentValue)
            {
                Circle.Draw(SharpDX.Color.Red, SpellHandler.Q.Range, Player.Instance);
            }
            if (SpellHandler.W.IsLearned && DrawingMenu.Get<CheckBox>("W").CurrentValue)
            {
                Circle.Draw(SharpDX.Color.PaleVioletRed, SpellHandler.W.Range, Player.Instance);
            }
            if (SpellHandler.W.IsLearned && DrawingMenu.Get<CheckBox>("W2").CurrentValue)
            {
                Circle.Draw(SharpDX.Color.PaleVioletRed, SpellHandler.WallOfPainMaxRange, Player.Instance);
            }
            if (SpellHandler.E.IsLearned && DrawingMenu.Get<CheckBox>("E").CurrentValue)
            {
                Circle.Draw(SharpDX.Color.OrangeRed, SpellHandler.E.Range, Player.Instance);
            }

            #endregion
        }

        private void OnTick(EventArgs args)
        {
            if (!Player.Instance.IsDead)
            {
                // Execute modes
                ModeHandler.OnTick();
            }
        }

        public T GetGlobal<T>(string indentifier) where T : ValueBase
        {
            T global = null;
            foreach (var menu in new[] { Menu }.Concat(ModeHandler.Modes.Select(o => o.Menu)))
            {
                global = menu.Get<T>(indentifier);
                if (global != null)
                {
                    break;
                }
            }
            return global;
        }

        public void Initialize()
        {
            // Only initialize once
            if (Initialized)
            {
                return;
            }
            Initialized = true;
        }
    }
}
