using System.Collections.Generic;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;

namespace Kindred
{
    public static class SpellManager
    {
        public const string EBuffName = "kindredecharge";
        public const string PassiveBuffName = "KindredHitTracker";

        public const uint QAcquisitionRange = 500;

        public static Spell.Targeted Q { get; private set; }
        public static Spell.Active W { get; private set; }
        public static Spell.Targeted E { get; private set; }
        public static Spell.Targeted R { get; private set; }

        public static IEnumerable<Spell.SpellBase> Spells { get; private set; }

        static SpellManager()
        {
            // Initialize spells
            Q = new Spell.Targeted(SpellSlot.Q, 340);
            W = new Spell.Active(SpellSlot.W, 900);
            E = new Spell.Targeted(SpellSlot.E, 500);
            R = new Spell.Targeted(SpellSlot.R, 500);

            Spells = (new Spell.SpellBase[] { Q, W, E, R }).OrderByDescending(o => o.Range).ToArray();
        }

        public static void Initialize()
        {
        }

        public static bool IsEnabled(this Spell.SpellBase spell, Orbwalker.ActiveModes mode)
        {
            switch (mode)
            {
                case Orbwalker.ActiveModes.Combo:
                    switch (spell.Slot)
                    {
                        case SpellSlot.Q:
                            return Config.Modes.Combo.UseQ;
                        case SpellSlot.W:
                            return Config.Modes.Combo.UseW;
                        case SpellSlot.E:
                            return Config.Modes.Combo.UseE;
                    }
                    break;
            }

            return false;
        }
    }
}
