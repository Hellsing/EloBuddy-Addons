using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;

// ReSharper disable InconsistentNaming
// ReSharper disable MemberHidesStaticFromOuterClass
namespace Hellsing.Kalista
{
    public static class Config
    {
        private const string MenuName = "Kalista";
        public static Menu Menu { get; private set; }

        static Config()
        {
            Menu = MainMenu.AddMenu(MenuName, "kalistaMenu");
            Menu.AddGroupLabel("Introduction");
            Menu.AddLabel("Welcome to my first EloBuddy addon!");

            // All modes
            Modes.Initialize();

            // Misc
            Misc.Initialize();

            // Items
            Items.Initialize();

            // Drawing
            // TODO
            //Drawing.Initialize();
        }

        public static void Initialize()
        {
        }

        public static class Modes
        {
            public static Menu Menu { get; private set; }

            static Modes()
            {
                // Initialize modes menu
                Menu = Config.Menu.AddSubMenu("Modes", "modes");

                // Combo
                Combo.Initialize();

                // Harass
                Menu.AddSeparator();
                Harass.Initialize();

                // WaveClear
                Menu.AddSeparator();
                WaveClear.Initialize();

                // JungleClear
                Menu.AddSeparator();
                JungleClear.Initialize();

                // Flee
                Menu.AddSeparator();
                Flee.Initialize();
            }

            public static void Initialize()
            { }

            public static class Combo
            {
                private static readonly CheckBox _useQ;
                public static bool UseQ { get { return _useQ.CurrentValue; } }

                private static readonly CheckBox _useE;
                public static bool UseE { get { return _useE.CurrentValue; } }

                private static readonly Slider _numE;
                public static int MinNumberE { get { return _numE.CurrentValue; } }

                private static readonly CheckBox _useItems;
                public static bool UseItems { get { return _useItems.CurrentValue; } }

                static Combo()
                {
                    Menu.AddGroupLabel("Combo");
                    _useQ = Menu.Add("comboUseQ", new CheckBox("Use Q"));
                    _useE = Menu.Add("comboUseE", new CheckBox("Use E"));
                    _useItems = Menu.Add("comboUseItems", new CheckBox("Use items"));
                    _numE = Menu.Add("comboNumE", new Slider("Min stacks to use E", 5, 1, 50));
                }

                public static void Initialize()
                { }
            }

            public static class Harass
            {
                private static readonly CheckBox _useQ;
                public static bool UseQ { get { return _useQ.CurrentValue; } }

                private static readonly Slider _mana;
                public static int MinMana { get { return _mana.CurrentValue; } }

                static Harass()
                {
                    Menu.AddGroupLabel("Harass");
                    _useQ = Menu.Add("harassUseQ", new CheckBox("Use Q"));
                    _mana = Menu.Add("harassMana", new Slider("Minimum mana in %", 30));
                }

                public static void Initialize()
                { }
            }

            public static class WaveClear
            {
                private static readonly CheckBox _useQ;
                public static bool UseQ { get { return _useQ.CurrentValue; } }

                private static readonly Slider _numQ;
                public static int MinNumberQ { get { return _numQ.CurrentValue; } }

                private static readonly CheckBox _useE;
                public static bool UseE { get { return _useE.CurrentValue; } }

                private static readonly Slider _numE;
                public static int MinNumberE { get { return _numE.CurrentValue; } }

                private static readonly Slider _mana;
                public static int MinMana { get { return _mana.CurrentValue; } }

                static WaveClear()
                {
                    Menu.AddGroupLabel("WaveClear");
                    _useQ = Menu.Add("waveUseQ", new CheckBox("Use Q"));
                    _useE = Menu.Add("waveUseE", new CheckBox("Use E"));
                    _numQ = Menu.Add("waveNumQ", new Slider("Minion kill number for Q", 3, 1, 10));
                    _numE = Menu.Add("waveNumE", new Slider("Minion kill number for E", 2, 1, 10));
                    Menu.AddSeparator();
                    _mana = Menu.Add("waveMana", new Slider("Minimum mana in %", 30));
                }

                public static void Initialize()
                { }
            }

            public static class JungleClear
            {
                private static readonly CheckBox _useE;
                public static bool UseE { get { return _useE.CurrentValue; } }

                static JungleClear()
                {
                    Menu.AddGroupLabel("JungleClear");
                    _useE = Menu.Add("jungleUseE", new CheckBox("Use E"));
                }

                public static void Initialize()
                { }
            }

            public static class Flee
            {
                private static readonly CheckBox _walljump;
                public static bool UseWallJumps { get { return _walljump.CurrentValue; } }

                private static readonly CheckBox _autoAttack;
                public static bool UseAutoAttacks { get { return _autoAttack.CurrentValue; } }

                static Flee()
                {
                    Menu.AddGroupLabel("Flee");
                    _walljump = Menu.Add("fleeWalljump", new CheckBox("Use WallJumps"));
                    _autoAttack = Menu.Add("fleeAutoattack", new CheckBox("Use AutoAttacks"));
                }

                public static void Initialize()
                { }
            }
        }

        public static class Misc
        {
            public static Menu Menu { get; private set; }

            private static CheckBox _killsteal;
            public static bool UseKillsteal { get { return _killsteal.CurrentValue; } }

            private static CheckBox _bigE;
            public static bool UseEBig { get { return _bigE.CurrentValue; } }

            private static CheckBox _saveSoulbound;
            public static bool SaveSouldBound { get { return _saveSoulbound.CurrentValue; } }

            private static CheckBox _secureE;
            public static bool SecureMinionKillsE { get { return _secureE.CurrentValue; } }

            private static CheckBox _harassPlus;
            public static bool UseHarassPlus { get { return _harassPlus.CurrentValue; } }

            public static void Initialize()
            {
                if (Menu == null)
                {
                    Menu = Config.Menu.AddSubMenu("Misc");

                    _killsteal = Menu.Add("killsteal", new CheckBox("Killsteal with E"));
                    _bigE = Menu.Add("bigE", new CheckBox("Always use E on big minions"));
                    _saveSoulbound = Menu.Add("saveSoulbound", new CheckBox("Use R to save your soulbound ally"));
                    _secureE = Menu.Add("secureE", new CheckBox("Use E to kill unkillable (AA) minions"));
                    _harassPlus = Menu.Add("harassPlus", new CheckBox("Auto E when a minion can die and enemies have 1+ stacks"));
                }
            }
        }

        public static class Items
        {
            public static Menu Menu { get; private set; }

            private static CheckBox _cutlass;
            public static bool UseCutlass { get { return _cutlass.CurrentValue; } }

            private static CheckBox _botrk;
            public static bool UseBotrk { get { return _botrk.CurrentValue; } }

            private static CheckBox _ghostblade;
            public static bool UseGhostblade { get { return _ghostblade.CurrentValue; } }

            public static void Initialize()
            {
                if (Menu == null)
                {
                    Menu = Config.Menu.AddSubMenu("Items");

                    _cutlass = Menu.Add("cutlass", new CheckBox("Use Bilgewater Cutlass"));
                    _botrk = Menu.Add("botrk", new CheckBox("Use Blade of the Ruined King"));
                    _ghostblade = Menu.Add("ghostblade", new CheckBox("Use Youmuu's Ghostblade"));
                }
            }
        }
    }
}
