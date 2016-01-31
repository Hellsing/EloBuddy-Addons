using System;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
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
            Drawing.Initialize();

            // Specials
            Specials.Initialize();
        }

        public static void Initialize()
        {
        }

        public static class Modes
        {
            private static Menu Menu { get; set; }

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
                LaneClear.Initialize();

                // JungleClear
                Menu.AddSeparator();
                JungleClear.Initialize();

                // Flee
                Menu.AddSeparator();
                Flee.Initialize();
            }

            public static void Initialize()
            {
            }

            public static class Combo
            {
                private static readonly CheckBox _useQ;
                private static readonly CheckBox _useQAA;
                private static readonly CheckBox _useAA;
                private static readonly CheckBox _useE;
                private static readonly CheckBox _useEslow;
                private static readonly Slider _numE;
                private static readonly Slider _mana;

                private static readonly CheckBox _useItems;

                public static bool UseQ
                {
                    get { return _useQ.CurrentValue; }
                }
                public static bool UseQAA
                {
                    get { return _useQAA.CurrentValue; }
                }
                public static bool UseE
                {
                    get { return _useE.CurrentValue; }
                }
                public static int MinNumberE
                {
                    get { return _numE.CurrentValue; }
                }
                public static bool UseAA
                {
                    get { return _useAA.CurrentValue; }
                }
                public static bool UseESlow
                {
                    get { return _useEslow.CurrentValue; }
                }
                public static bool UseItems
                {
                    get { return _useItems.CurrentValue; }
                }
                public static int ManaQ
                {
                    get { return _mana.CurrentValue; }
                }

                static Combo()
                {
                    Menu.AddGroupLabel("Combo");

                    _useQ = Menu.Add("comboUseQ", new CheckBox("Use Q"));
                    _useQAA = Menu.Add("comboUseQAA", new CheckBox("Use Q after auto attack only"));
                    _useE = Menu.Add("comboUseE", new CheckBox("Use E"));
                    _useEslow = Menu.Add("comboUseEslow", new CheckBox("Kill minions with E to slow enemy"));
                    _useAA = Menu.Add("comboUseAA", new CheckBox("Attack minions to gap close"));
                    _useItems = Menu.Add("comboUseItems", new CheckBox("Use items"));
                    _numE = Menu.Add("comboNumE", new Slider("Mininum stacks to use E", 5, 1, 50));
                    _mana = Menu.Add("comboMana", new Slider("Mana usage for Q in percent ({0}%)", 30));
                }

                public static void Initialize()
                {
                }
            }

            public static class Harass
            {
                private static readonly CheckBox _useQ;
                private static readonly Slider _mana;

                public static bool UseQ
                {
                    get { return _useQ.CurrentValue; }
                }
                public static int MinMana
                {
                    get { return _mana.CurrentValue; }
                }

                static Harass()
                {
                    Menu.AddGroupLabel("Harass");

                    _useQ = Menu.Add("harassUseQ", new CheckBox("Use Q"));
                    _mana = Menu.Add("harassMana", new Slider("Minimum mana in %", 30));
                }

                public static void Initialize()
                {
                }
            }

            public static class LaneClear
            {
                private static readonly CheckBox _useQ;
                private static readonly Slider _numQ;
                private static readonly CheckBox _useE;
                private static readonly Slider _numE;
                private static readonly Slider _mana;

                public static bool UseQ
                {
                    get { return _useQ.CurrentValue; }
                }
                public static int MinNumberQ
                {
                    get { return _numQ.CurrentValue; }
                }
                public static bool UseE
                {
                    get { return _useE.CurrentValue; }
                }
                public static int MinNumberE
                {
                    get { return _numE.CurrentValue; }
                }
                public static int MinMana
                {
                    get { return _mana.CurrentValue; }
                }

                static LaneClear()
                {
                    Menu.AddGroupLabel("LaneClear");

                    _useQ = Menu.Add("laneUseQ", new CheckBox("Use Q"));
                    _useE = Menu.Add("laneUseE", new CheckBox("Use E"));
                    _numQ = Menu.Add("laneNumQ", new Slider("Minion kill number for Q", 3, 1, 10));
                    _numE = Menu.Add("laneNumE", new Slider("Minion kill number for E", 2, 1, 10));
                    Menu.AddSeparator();
                    _mana = Menu.Add("laneMana", new Slider("Minimum mana in %", 30));
                }

                public static void Initialize()
                {
                }
            }

            public static class JungleClear
            {
                private static readonly CheckBox _useE;

                public static bool UseE
                {
                    get { return _useE.CurrentValue; }
                }

                static JungleClear()
                {
                    Menu.AddGroupLabel("JungleClear");

                    _useE = Menu.Add("jungleUseE", new CheckBox("Use E"));
                }

                public static void Initialize()
                {
                }
            }

            public static class Flee
            {
                private static readonly CheckBox _walljump;
                private static readonly CheckBox _autoAttack;

                public static bool UseWallJumps
                {
                    get { return _walljump.CurrentValue; }
                }
                public static bool UseAutoAttacks
                {
                    get { return _autoAttack.CurrentValue; }
                }

                static Flee()
                {
                    Menu.AddGroupLabel("Flee");

                    _walljump = Menu.Add("fleeWalljump", new CheckBox("Use WallJumps"));
                    _autoAttack = Menu.Add("fleeAutoattack", new CheckBox("Use AutoAttacks"));
                }

                public static void Initialize()
                {
                }
            }
        }

        public static class Misc
        {
            private static Menu Menu { get; set; }

            private static readonly CheckBox _killsteal;
            private static readonly CheckBox _bigE;
            private static readonly CheckBox _saveSoulbound;
            private static readonly CheckBox _secureE;
            private static readonly CheckBox _harassPlus;
            private static readonly Slider _autoBelowHealthE;
            private static readonly Slider _reductionE;

            public static bool UseKillsteal
            {
                get { return _killsteal.CurrentValue; }
            }
            public static bool UseEBig
            {
                get { return _bigE.CurrentValue; }
            }
            public static bool SaveSouldBound
            {
                get { return _saveSoulbound.CurrentValue; }
            }
            public static bool SecureMinionKillsE
            {
                get { return _secureE.CurrentValue; }
            }
            public static bool UseHarassPlus
            {
                get { return _harassPlus.CurrentValue; }
            }
            public static int AutoEBelowHealth
            {
                get { return _autoBelowHealthE.CurrentValue; }
            }
            public static int DamageReductionE
            {
                get { return _reductionE.CurrentValue; }
            }

            static Misc()
            {
                Menu = Config.Menu.AddSubMenu("Misc");

                Menu.AddGroupLabel("Misc features");
                _killsteal = Menu.Add("killsteal", new CheckBox("Killsteal with E"));
                _bigE = Menu.Add("bigE", new CheckBox("Always use E on big minions"));
                _saveSoulbound = Menu.Add("saveSoulbound", new CheckBox("Use R to save your soulbound ally"));
                _secureE = Menu.Add("secureE", new CheckBox("Use E to kill unkillable (AA) minions"));
                _harassPlus = Menu.Add("harassPlus", new CheckBox("Auto E when a minion can die and enemies have 1+ stacks"));
                _autoBelowHealthE = Menu.Add("autoBelowHealthE", new Slider("Auto E when our health is below ({0}%) percent", 10));
                _reductionE = Menu.Add("reductionE", new Slider("Reduce E damage by {0} points", 20));

                // Initialize other misc features
                Sentinel.Initialize();
            }

            public static void Initialize()
            {
            }

            public static class Sentinel
            {
                private static readonly CheckBox _enabled;
                private static readonly CheckBox _noMode;
                private static readonly CheckBox _alert;
                private static readonly Slider _mana;

                private static readonly CheckBox _baron;
                private static readonly CheckBox _dragon;
                private static readonly CheckBox _mid;
                private static readonly CheckBox _blue;
                private static readonly CheckBox _red;

                public static bool Enabled
                {
                    get { return _enabled.CurrentValue; }
                }
                public static bool NoModeOnly
                {
                    get { return _noMode.CurrentValue; }
                }
                public static bool Alert
                {
                    get { return _alert.CurrentValue; }
                }
                public static int Mana
                {
                    get { return _mana.CurrentValue; }
                }

                public static bool SendBaron
                {
                    get { return _baron.CurrentValue; }
                }
                public static bool SendDragon
                {
                    get { return _dragon.CurrentValue; }
                }
                public static bool SendMid
                {
                    get { return _mid.CurrentValue; }
                }
                public static bool SendBlue
                {
                    get { return _blue.CurrentValue; }
                }
                public static bool SendRed
                {
                    get { return _red.CurrentValue; }
                }

                static Sentinel()
                {
                    Menu.AddGroupLabel("Sentinel (W) usage");

                    if (Game.MapId != GameMapId.SummonersRift)
                    {
                        Menu.AddLabel("Only on Summoners Rift, sorry.");
                    }
                    else
                    {
                        _enabled = Menu.Add("enabled", new CheckBox("Enabled"));
                        _noMode = Menu.Add("noMode", new CheckBox("Only use when no mode active"));
                        _alert = Menu.Add("alert", new CheckBox("Alert when sentinel is taking damage"));
                        _mana = Menu.Add("mana", new Slider("Minimum mana available when casting W ({0}%)", 40));

                        Menu.AddLabel("Send to the following locations (no specific order):");
                        (_baron = Menu.Add("baron", new CheckBox("Baron (stuck bug usage)"))).OnValueChange += OnValueChange;
                        (_dragon = Menu.Add("dragon", new CheckBox("Dragon (stuck bug usage)"))).OnValueChange += OnValueChange;
                        (_mid = Menu.Add("mid", new CheckBox("Mid lane brush"))).OnValueChange += OnValueChange;
                        (_blue = Menu.Add("blue", new CheckBox("Blue buff"))).OnValueChange += OnValueChange;
                        (_red = Menu.Add("red", new CheckBox("Red buff"))).OnValueChange += OnValueChange;
                        SentinelManager.RecalculateOpenLocations();
                    }
                }

                private static void OnValueChange(ValueBase<bool> sender, ValueBase<bool>.ValueChangeArgs args)
                {
                    SentinelManager.RecalculateOpenLocations();
                }

                public static void Initialize()
                {
                }
            }
        }

        public static class Items
        {
            private static Menu Menu { get; set; }

            private static readonly CheckBox _cutlass;
            private static readonly CheckBox _botrk;
            private static readonly CheckBox _ghostblade;

            public static bool UseCutlass
            {
                get { return _cutlass.CurrentValue; }
            }
            public static bool UseBotrk
            {
                get { return _botrk.CurrentValue; }
            }
            public static bool UseGhostblade
            {
                get { return _ghostblade.CurrentValue; }
            }

            static Items()
            {
                Menu = Config.Menu.AddSubMenu("Items");

                _cutlass = Menu.Add("cutlass", new CheckBox("Use Bilgewater Cutlass"));
                _botrk = Menu.Add("botrk", new CheckBox("Use Blade of the Ruined King"));
                _ghostblade = Menu.Add("ghostblade", new CheckBox("Use Youmuu's Ghostblade"));
            }

            public static void Initialize()
            {
            }
        }

        public static class Drawing
        {
            private static Menu Menu { get; set; }

            private static readonly CheckBox _drawQ;
            private static readonly CheckBox _drawW;
            private static readonly CheckBox _drawE;
            private static readonly CheckBox _drawEleaving;
            private static readonly CheckBox _drawR;

            private static readonly CheckBox _healthbar;
            private static readonly CheckBox _percent;

            public static bool DrawQ
            {
                get { return _drawQ.CurrentValue; }
            }
            public static bool DrawW
            {
                get { return _drawW.CurrentValue; }
            }
            public static bool DrawE
            {
                get { return _drawE.CurrentValue; }
            }
            public static bool DrawELeaving
            {
                get { return _drawEleaving.CurrentValue; }
            }
            public static bool DrawR
            {
                get { return _drawR.CurrentValue; }
            }
            public static bool IndicatorHealthbar
            {
                get { return _healthbar.CurrentValue; }
            }
            public static bool IndicatorPercent
            {
                get { return _percent.CurrentValue; }
            }

            static Drawing()
            {
                Menu = Config.Menu.AddSubMenu("Drawing");

                Menu.AddGroupLabel("Spell ranges");
                _drawQ = Menu.Add("drawQ", new CheckBox("Q range"));
                _drawW = Menu.Add("drawW", new CheckBox("W range"));
                _drawE = Menu.Add("drawE", new CheckBox("E range"));
                _drawEleaving = Menu.Add("drawEleaving", new CheckBox("E trigger range (see combo)", false));
                _drawR = Menu.Add("drawR", new CheckBox("R range", false));

                Menu.AddGroupLabel("Damage indicators (Rend - E)");
                _healthbar = Menu.Add("healthbar", new CheckBox("Healthbar overlay"));
                _percent = Menu.Add("percent", new CheckBox("Damage percent info"));
            }

            public static void Initialize()
            {
            }
        }

        public static class Specials
        {
            private static Menu Menu { get; set; }

            private static CheckBox _useBalista;
            private static CheckBox _balistaComboOnly;
            private static CheckBox _balistaMoreHealth;
            private static Slider _balistaTriggerRange;

            public static bool UseBalista
            {
                get { return _useBalista != null && _useBalista.CurrentValue; }
            }
            public static bool BalistaComboOnly
            {
                get { return _balistaComboOnly.CurrentValue; }
            }
            public static bool BalistaMoreHealthOnly
            {
                get { return _balistaMoreHealth.CurrentValue; }
            }
            public static int BalistaTriggerRange
            {
                get { return _balistaTriggerRange.CurrentValue; }
            }

            static Specials()
            {
                Menu = Config.Menu.AddSubMenu("Specials");

                Menu.AddGroupLabel("Balista");
                if (EntityManager.Heroes.Allies.Any(o => o.ChampionName == "Blitzcrank"))
                {
                    Menu.Add("infoLabel", new Label("You have no soul bound yet!"));
                    Game.OnTick += BalistaCheckSoulBound;
                }
                else
                {
                    Menu.AddLabel("You don't have Blitzcrank on your team, so no Balista :(");
                }
            }

            private static void BalistaCheckSoulBound(EventArgs args)
            {
                if (SoulBoundSaver.SoulBound != null)
                {
                    Game.OnTick -= BalistaCheckSoulBound;
                    Menu.Remove("infoLabel");

                    if (SoulBoundSaver.SoulBound.ChampionName != "Blitzcrank")
                    {
                        Menu.AddLabel("You are not bound to Blitzcrank!");
                        Menu.AddLabel("If you decided to rebind, please reload the addon to make sure");
                        Menu.AddLabel("it recognizes your new bind! Now have fun playing!");
                        return;
                    }

                    _useBalista = Menu.Add("useBalista", new CheckBox("Enabled"));
                    Menu.AddSeparator(0);
                    _balistaComboOnly = Menu.Add("balistaComboOnly", new CheckBox("Combo mode only", false));
                    _balistaMoreHealth = Menu.Add("moreHealth", new CheckBox("Only if I have more health"));

                    const int blitzcrankQrange = 925;
                    _balistaTriggerRange = Menu.Add("balistaTriggerRange",
                        new Slider("Trigger range between you and the grabbed target", (int) SpellManager.R.Range, (int) SpellManager.R.Range,
                            (int) (SpellManager.R.Range + blitzcrankQrange * 0.8f)));

                    // Handle Blitzcrank hooks in Kalista.OnTickBalistaCheck
                    Obj_AI_Base.OnBuffGain += delegate(Obj_AI_Base sender, Obj_AI_BaseBuffGainEventArgs eventArgs)
                    {
                        if (eventArgs.Buff.DisplayName == "RocketGrab" && eventArgs.Buff.Caster.NetworkId == SoulBoundSaver.SoulBound.NetworkId)
                        {
                            Game.OnTick += Kalista.OnTickBalistaCheck;
                        }
                    };
                    Obj_AI_Base.OnBuffLose += delegate(Obj_AI_Base sender, Obj_AI_BaseBuffLoseEventArgs eventArgs)
                    {
                        if (eventArgs.Buff.DisplayName == "RocketGrab" && eventArgs.Buff.Caster.NetworkId == SoulBoundSaver.SoulBound.NetworkId)
                        {
                            Game.OnTick -= Kalista.OnTickBalistaCheck;
                        }
                    };
                }
            }

            public static void Initialize()
            {
            }
        }
    }
}
