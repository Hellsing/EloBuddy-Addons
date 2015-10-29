using System;
using System.Linq;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;

// ReSharper disable InconsistentNaming
// ReSharper disable MemberHidesStaticFromOuterClass

namespace Kindred
{
    public static class Config
    {
        public const string MenuName = "Kindred";
        private static readonly Menu Menu;

        static Config()
        {
            // Initialize menu
            Menu = MainMenu.AddMenu(MenuName, MenuName + "_hellsing");

            // Initialize sub menus
            Modes.Initialize();
            Misc.Initialize();
        }

        public static void Initialize()
        {
        }

        public static class Modes
        {
            public const string MenuName = "Modes";
            private static readonly Menu Menu;

            static Modes()
            {
                // Initialize menu
                Menu = Config.Menu.AddSubMenu(MenuName);

                // Initialize sub groups
                Combo.Initialize();
                Menu.AddSeparator();
                Harass.Initialize();
                Menu.AddSeparator();
                LaneClear.Initialize();
                Menu.AddSeparator();
                JungleClear.Initialize();
                Menu.AddSeparator();
                Flee.Initialize();
            }

            public static void Initialize()
            {
            }

            public static class Combo
            {
                public const string GroupName = "Combo";

                private static readonly CheckBox _useQ;
                private static readonly CheckBox _useW;
                private static readonly CheckBox _useE;
                private static readonly CheckBox _useItems;

                private static readonly CheckBox _advancedE;

                private static readonly CheckBox _gapcloseQ;
                private static readonly Slider _triggerDistanceW;

                public static bool UseQ
                {
                    get { return _useQ.CurrentValue; }
                }
                public static bool UseW
                {
                    get { return _useW.CurrentValue; }
                }
                public static bool UseE
                {
                    get { return _useE.CurrentValue; }
                }
                public static bool UseItems
                {
                    get { return _useItems.CurrentValue; }
                }

                public static bool UseAdvancedE
                {
                    get { return _advancedE.CurrentValue; }
                }

                public static bool GaploseQBeforeW
                {
                    get { return _useQ.CurrentValue && _gapcloseQ.CurrentValue; }
                }
                public static int TriggerDistanceW
                {
                    get { return _triggerDistanceW.CurrentValue; }
                }

                static Combo()
                {
                    // Initialize group
                    Menu.AddGroupLabel(GroupName);

                    _useQ = Menu.Add("comboUseQ", new CheckBox("Use Q"));
                    _useW = Menu.Add("comboUseW", new CheckBox("Use W"));
                    _useE = Menu.Add("comboUseE", new CheckBox("Use E"));
                    _useItems = Menu.Add("comboUseItems", new CheckBox("Use items"));

                    Menu.AddLabel("Advanced features:");

                    _advancedE = Menu.Add("comboAdvancedE", new CheckBox("Only use E if 3 hits can land"));
                    _gapcloseQ = Menu.Add("comboGapcloseQ", new CheckBox("Gapclose with Q before using W"));
                    _triggerDistanceW = Menu.Add("comboDistanceW", new Slider("Trigger distance to the target for using W",
                        (int) SpellManager.W.Range / 2, // Default
                        (int) SpellManager.W.Range / 5, // Min
                        (int) SpellManager.W.Range)); // Max
                }

                public static void Initialize()
                {
                }
            }

            public static class Harass
            {
                public const string GroupName = "Harass";

                private static readonly CheckBox _useW;

                public static bool UseW
                {
                    get { return _useW.CurrentValue; }
                }

                static Harass()
                {
                    // Initialize group
                    Menu.AddGroupLabel(GroupName);

                    _useW = Menu.Add("harassUseW", new CheckBox("Use W"));
                }

                public static void Initialize()
                {
                }
            }

            public static class LaneClear
            {
                static LaneClear()
                {
                }

                public static void Initialize()
                {
                }
            }

            public static class JungleClear
            {
                static JungleClear()
                {
                }

                public static void Initialize()
                {
                }
            }

            public static class Flee
            {
                static Flee()
                {
                }

                public static void Initialize()
                {
                }
            }
        }

        public static class Misc
        {
            public const string MenuName = "Miscellaneous";
            private static readonly Menu Menu;

            private static readonly Slider _directionQ;

            public static DirectionQ QDirection
            {
                get { return (DirectionQ) _directionQ.CurrentValue; }
            }

            static Misc()
            {
                // Initialize menu
                Menu = Config.Menu.AddSubMenu(MenuName);

                Menu.AddGroupLabel("Q Settings");
                (_directionQ = new Slider("Jump direction: " + ((DirectionQ) 0), 0, 0, Enum.GetValues(typeof (DirectionQ)).Length - 1)).OnValueChange +=
                    delegate { _directionQ.DisplayName = "Jump direction: " + ((DirectionQ) _directionQ.CurrentValue); };
                Menu.Add("directionQ", _directionQ);
                Menu.AddLabel("Available directions:");
                foreach (var direction in Enum.GetValues(typeof (DirectionQ)).Cast<DirectionQ>())
                {
                    Menu.AddLabel(string.Format("    {0}: {1}", (int) direction, direction));
                }
            }

            public static void Initialize()
            {
            }

            public enum DirectionQ
            {
                ToTarget,
                ToMouse
            }
        }
    }
}
