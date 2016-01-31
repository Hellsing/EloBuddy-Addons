using System.Linq;
using EloBuddy;
using EloBuddy.SDK;

namespace Gnaaar
{
    public static class Damages
    {
        public static float GetTotalDamage(AIHeroClient target)
        {
            // Auto attack
            var damage = Player.Instance.GetAutoAttackDamage(target);

            // Q
            if (SpellManager.Q.IsReady())
                damage += SpellManager.Q.GetRealDamage(target);

            // W
            if (SpellManager.W.IsReady())
                damage += SpellManager.W.GetRealDamage(target);

            // E
            if (SpellManager.E.IsReady())
                damage += SpellManager.E.GetRealDamage(target);

            // R
            if (SpellManager.R.IsReady())
                damage += SpellManager.R.GetRealDamage(target);

            return damage;
        }

        public enum TransformStates
        {
            Automatic,
            Mini,
            Mega
        }

        public static float GetRealDamage(this Spell.SpellBase spell, Obj_AI_Base target)
        {
            return GetRealDamage(spell.Slot, target, spell.IsMiniSpell() ? TransformStates.Mini : TransformStates.Mega);
        }

        public static float GetRealDamage(SpellSlot slot, Obj_AI_Base target, TransformStates state = TransformStates.Automatic)
        {
            // Helpers
            var spellLevel = Player.Instance.Spellbook.GetSpell(slot).Level;
            var damageType = DamageType.Physical;
            float damage = 0;
            float extraDamage = 0;

            // Validate spell level
            if (spellLevel == 0)
                return 0;
            spellLevel--;

            switch (slot)
            {
                case SpellSlot.Q:

                    if (state == TransformStates.Mini || state == TransformStates.Automatic && Player.Instance.IsMiniGnar())
                    {
                        // Throws a boomerang that deals 5/35/65/95/125 (+1.15) physical damage and slows enemies by 15/20/25/30/35% for 2 seconds.
                        // The boomerang returns towards Gnar after hitting an enemy, dealing 50% damage to subsequent targets. Each enemy can only be hit once.
                        damage = new[] { 5, 35, 65, 95, 125 }[spellLevel] + 1.15f * Player.Instance.TotalAttackDamage;
                    }
                    else if (state == TransformStates.Mega || state == TransformStates.Automatic && Player.Instance.IsMegaGnar())
                    {
                        // Throws a boulder that stops when it hits an enemy, slowing all nearby enemies and dealing 5/45/85/125/165 (+1.2) physical damage.
                        damage = new[] { 5, 45, 85, 125, 165 }[spellLevel] + 1.2f * (Player.Instance.BaseAttackDamage + Player.Instance.FlatPhysicalDamageMod);
                    }

                    break;

                case SpellSlot.W:

                    if (state == TransformStates.Mini || state == TransformStates.Automatic && Player.Instance.IsMiniGnar())
                    {
                        // Every 3rd attack or spell on the same target deals an additional 10/20/30/40/50 (+1) + 6/8/10/12/14% of the target's max Health as magic damage
                        // and grants Gnar undefined% Movement Speed that decays over 3 seconds (max 100/150/200/250/300 damage vs. monsters). 
                        var buff = target.Buffs.FirstOrDefault(b => b.IsActive && Game.Time < b.EndTime && b.DisplayName == "GnarWProc" && b.Caster.NetworkId == Player.Instance.NetworkId);
                        if (buff != null && buff.Count == 2)
                        {
                            damageType = DamageType.Magical;
                            damage = new[] { 10, 20, 30, 40, 50 }[spellLevel] + Player.Instance.TotalMagicalDamage + new[] { 0.06f, 0.08f, 0.1f, 0.12f, 0.14f }[spellLevel] * target.MaxHealth;

                            // Special case for minions
                            if (target is Obj_AI_Minion)
                            {
                                var maxDamage = new[] { 100, 150, 200, 250, 300 }[spellLevel];
                                if (Player.Instance.CalculateDamageOnUnit(target, damageType, damage) > maxDamage)
                                {
                                    damageType = DamageType.True;
                                    damage = maxDamage;
                                }
                            }
                        }
                    }
                    else if (state == TransformStates.Mega || state == TransformStates.Automatic && Player.Instance.IsMegaGnar())
                    {
                        // Stuns enemies in an area for 1.25 seconds, dealing 25/45/65/85/105 (+1) physical damage.
                        damage = new[] { 25, 45, 65, 85, 105 }[spellLevel] + (Player.Instance.BaseAttackDamage + Player.Instance.FlatPhysicalDamageMod);
                    }

                    break;

                case SpellSlot.E:

                    if (state == TransformStates.Mini || state == TransformStates.Automatic && Player.Instance.IsMiniGnar())
                    {
                        // Leaps to a location, gaining 20/30/40/50/60% Attack Speed for 3 seconds. If Gnar lands on a unit he will bounce off it, traveling further.
                        // Deals 20/60/100/140/180 (+undefined) [6% of Gnar's Max Health] physical damage and slows briefly if the unit landed on was an enemy.
                        damage = new[] { 20, 60, 100, 140, 180 }[spellLevel] + 0.06f * Player.Instance.MaxHealth;
                    }
                    else if (state == TransformStates.Mega || state == TransformStates.Automatic && Player.Instance.IsMegaGnar())
                    {
                        // Leaps to a location and deals 20/60/100/140/180 (+undefined) [6% of Gnar's Max Health] physical damage to all nearby enemies on landing.
                        // Enemies Gnar lands directly on top of are slowed briefly.
                        damage = new[] { 20, 60, 100, 140, 180 }[spellLevel] + 0.06f * Player.Instance.MaxHealth;
                    }

                    break;

                case SpellSlot.R:

                    if (state == TransformStates.Mega || state == TransformStates.Automatic && Player.Instance.IsMegaGnar())
                    {
                        // Knocks all nearby enemies in the specified direction, dealing 200/300/400 (+0.2) (+0.5) physical damage and slowing them by 45% for 1.25/1.5/1.75 seconds.
                        // Any enemy that hits a wall takes 150% damage and is stunned instead of slowed.
                        damage = new[] { 200, 300, 400 }[spellLevel] + 0.2f * Player.Instance.TotalAttackDamage;
                        extraDamage = Player.Instance.CalculateDamageOnUnit(target, DamageType.Magical, Player.Instance.BaseAbilityDamage + Player.Instance.FlatMagicDamageMod);
                    }

                    break;
            }

            // No damage set
            if (damage <= 0 && extraDamage <= 0)
            {
                return 0;
            }

            // Calculate damage on target and return (-20 to make it actually more accurate Kappa)
            return Player.Instance.CalculateDamageOnUnit(target, damageType, damage) + extraDamage - 20;
        }
    }
}
