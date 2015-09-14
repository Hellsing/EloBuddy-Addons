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
            var hero = target as AIHeroClient;
            return GetRendDamage(target) > target.Health && (hero == null || !hero.HasUndyingBuff());
        }

        public static float GetRendDamage(AIHeroClient target)
        {
            return GetRendDamage(target, -1);
        }

        public static float GetRendDamage(Obj_AI_Base target, int customStacks = -1)
        {
            // Calculate the damage and return
            return (Player.Instance.CalculateDamageOnUnit(target, DamageType.Physical, GetRawRendDamage(target, customStacks)) - 20) * 0.98f;
        }

        // ReSharper disable once PossibleNullReferenceException
        public static float GetRawRendDamage(Obj_AI_Base target, int customStacks = -1)
        {
            // Get buff
            var buff = target.GetRendBuff();

            if (buff != null || customStacks > -1)
            {
                return (RawRendDamage[SpellManager.E.Level - 1] + RawRendDamageMultiplier[SpellManager.E.Level - 1] * Player.Instance.TotalAttackDamage) + // Base damage
                       ((customStacks < 0 ? buff.Count : customStacks) - 1) * // Spear count
                       (RawRendDamagePerSpear[SpellManager.E.Level - 1] + RawRendDamagePerSpearMultiplier[SpellManager.E.Level - 1] * Player.Instance.TotalAttackDamage); // Damage per spear
            }

            return 0;
        }

        public static float GetTotalDamage(AIHeroClient target)
        {
            // Auto attack damage
            var damage = Player.Instance.GetAutoAttackDamage(target);

            // Q damage
            if (SpellManager.Q.IsReady())
            {
                damage += QDamage.GetDamage(target);
            }

            // E stack damage
            if (SpellManager.E.IsReady())
            {
                damage += GetRendDamage(target);
            }

            return damage;
        }
    }
}
