using EloBuddy;
using EloBuddy.SDK;

namespace Karthus
{
    public static class Damages
    {
        public static float GetTotalDamage(Karthus instance, AIHeroClient target)
        {
            // Auto attack
            var damage = Player.Instance.GetAutoAttackDamage(target);

            // Q
            if (instance.SpellHandler.Q.IsReady())
            {
                damage += instance.SpellHandler.Q.GetRealDamage(target);
            }

            // E
            if (instance.SpellHandler.E.IsReady() && instance.SpellHandler.E.IsInRange(target))
            {
                damage += instance.SpellHandler.E.GetRealDamage(target);
            }

            // R
            if (instance.SpellHandler.R.IsReady())
            {
                damage += instance.SpellHandler.R.GetRealDamage(target);
            }

            return damage;
        }

        public static float GetRealDamage(this Spell.SpellBase spell, Obj_AI_Base target)
        {
            return spell.Slot.GetRealDamage(target);
        }

        public static float GetRealDamage(this SpellSlot slot, Obj_AI_Base target)
        {
            // Helpers
            var spellLevel = Player.Instance.Spellbook.GetSpell(slot).Level;
            const DamageType damageType = DamageType.Magical;
            float damage = 0;

            // Validate spell level
            if (spellLevel == 0)
            {
                return 0;
            }
            spellLevel--;

            switch (slot)
            {
                case SpellSlot.Q:

                    // Karthus detonates the target area in the cursor's direction after a 0.5 seconds delay,
                    // dealing magic damage to all units within, doubled when hitting only a single target.
                    damage = new float[] { 40, 60, 80, 100, 120 }[spellLevel] + 0.3f * Player.Instance.TotalMagicalDamage;
                    break;

                case SpellSlot.E:

                    // Karthus deals magic damage per second to all nearby units.
                    damage = new float[] { 30, 50, 70, 90, 110 }[spellLevel] + 0.2f * Player.Instance.TotalMagicalDamage;
                    break;

                case SpellSlot.R:

                    // Karthus channels for 3 seconds and deals magic damage to all enemy champions upon its completion.
                    damage = new float[] { 250, 400, 550 }[spellLevel] + 0.6f * Player.Instance.TotalMagicalDamage;
                    break;
            }

            // No damage set
            if (damage <= 0)
            {
                return 0;
            }

            // Calculate damage on target and return (-20 to make it actually more accurate Kappa)
            return Player.Instance.CalculateDamageOnUnit(target, damageType, damage) - 20;
        }
    }
}
