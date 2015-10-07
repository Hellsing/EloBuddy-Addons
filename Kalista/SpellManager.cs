using System.Collections.Generic;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Enumerations;
using SharpDX;

namespace Hellsing.Kalista
{
    public static class SpellManager
    {
        public static Spell.Skillshot Q { get; private set; }
        public static Spell.Targeted W { get; private set; }
        public static Spell.Active E { get; private set; }
        public static Spell.Active R { get; private set; }

        public static List<Spell.SpellBase> AllSpells { get; private set; }

        public static Dictionary<SpellSlot, Color> ColorTranslation { get; private set; }

        static SpellManager()
        {
            // Initialize spells
            Q = new Spell.Skillshot(SpellSlot.Q, 1150, SkillShotType.Linear, 250, 1200, 40);
            W = new Spell.Targeted(SpellSlot.W, 5000);
            E = new Spell.Active(SpellSlot.E, 1000);
            R = new Spell.Active(SpellSlot.R, 1500);

            AllSpells = new List<Spell.SpellBase>(new Spell.SpellBase[] { Q, W, E, R });
            ColorTranslation = new Dictionary<SpellSlot, Color>
            {
                { SpellSlot.Q, Color.IndianRed.ToArgb(150) },
                { SpellSlot.W, Color.MediumPurple.ToArgb(150) },
                { SpellSlot.E, Color.DarkRed.ToArgb(150) },
                { SpellSlot.R, Color.Red.ToArgb(150) }
            };
        }

        private static Color ToArgb(this Color color, byte a)
        {
            return new ColorBGRA(color.R, color.G, color.B, a);
        }

        public static Color GetColor(this Spell.SpellBase spell)
        {
            return ColorTranslation.ContainsKey(spell.Slot) ? ColorTranslation[spell.Slot] : Color.Wheat;
        }
    }
}
