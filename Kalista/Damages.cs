using EloBuddy;
using EloBuddy.SDK;

namespace Hellsing.Kalista
{
    public static class Damages
    {
        public static readonly Damage.DamageSourceBoundle QDamage = new Damage.DamageSourceBoundle();

        private static readonly float[] RawRendDamage = { 20, 30, 40, 50, 60 };
        private static readonly float[] RawRendDamageMultiplier = { 0.6f, 0.6f, 0.6f, 0.6f, 0.6f };
        private static readonly float[] RawRendDamagePerSpear = { 10, 14, 19, 25, 32 };
        private static readonly float[] RawRendDamagePerSpearMultiplier = { 0.2f, 0.225f, 0.25f, 0.275f, 0.3f };

        static Damages()
        {
            QDamage.Add(new Damage.DamageSource(SpellSlot.Q, DamageType.Physical)
            {
                Damages = new float[] { 10, 70, 130, 190, 250 }
            });
            QDamage.Add(new Damage.BonusDamageSource(SpellSlot.Q, DamageType.Physical)
            {
                DamagePercentages = new float[] { 1, 1, 1, 1, 1 }
            });
        }

        public static bool IsRendKillable(this Obj_AI_Base target)
        {
            // Validate unit
            if (target == null || !target.IsValidTarget() || !target.HasRendBuff())
            {
                return false;
            }

            // Take into account all kinds of shields
            var totalHealth = target.TotalShieldHealth();

            var hero = target as AIHeroClient;
            if (hero != null)
            {
                // Validate that target has no undying buff or spellshield
                if (hero.HasUndyingBuff() || hero.HasSpellShield())
                {
                    return false;
                }

                // Take into account Blitzcranks passive
                if (hero.ChampionName == "Blitzcrank" && !target.HasBuff("BlitzcrankManaBarrierCD") && !target.HasBuff("ManaBarrier"))
                {
                    totalHealth += target.Mana / 2;
                }
            }

            return GetRendDamage(target) > totalHealth;
        }

        public static float GetRendDamage(AIHeroClient target)
        {
            return GetRendDamage(target, -1);
        }

        public static float GetRendDamage(Obj_AI_Base target, int customStacks = -1)
        {
            // Calculate the damage and return
            return Player.Instance.CalculateDamageOnUnit(target, DamageType.Physical, GetRawRendDamage(target, customStacks) - Config.Misc.DamageReductionE); 
        }

        public static float GetRawRendDamage(Obj_AI_Base target, int customStacks = -1)
        {
            var stacks = (customStacks > -1 ? customStacks : target.HasRendBuff() ? target.GetRendBuff().Count : 0) - 1;
            if (stacks > -1)
            {
                var index = SpellManager.E.Level - 1;
                return RawRendDamage[index] + stacks * RawRendDamagePerSpear[index] +
                       Player.Instance.TotalAttackDamage * (RawRendDamageMultiplier[index] + stacks * RawRendDamagePerSpearMultiplier[index]);
            }

            return 0;
        }
    }
}
