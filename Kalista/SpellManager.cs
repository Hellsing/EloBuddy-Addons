using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Enumerations;

namespace Hellsing.Kalista
{
    public static class SpellManager
    {
        public static Spell.Skillshot Q { get; private set; }
        public static Spell.Targeted W { get; private set; }
        public static Spell.Active E { get; private set; }
        public static Spell.Active R { get; private set; }

        static SpellManager()
        {
            // Initialize spells
            Q = new Spell.Skillshot(SpellSlot.Q, 1200, SkillShotType.Linear, 350, 2400, 40);
            W = new Spell.Targeted(SpellSlot.W, 5000);
            E = new Spell.Active(SpellSlot.E, 1000);
            R = new Spell.Active(SpellSlot.R, 1500);
        }

        public static Spell.SpellBase GetSpellFromSlot(SpellSlot slot)
        {
            switch (slot)
            {
                case SpellSlot.Q:
                    return Q;
                case SpellSlot.W:
                    return W;
                case SpellSlot.E:
                    return E;
                case SpellSlot.R:
                    return R;
            }
            return null;
        }
    }
}
