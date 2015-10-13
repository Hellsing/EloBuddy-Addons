using System.Collections.Generic;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;

namespace Hellsing.Kalista
{
    public static class Extensions
    {
        public static bool HasRendBuff(this Obj_AI_Base target)
        {
            return target.GetRendBuff() != null;
        }

        public static BuffInstance GetRendBuff(this Obj_AI_Base target)
        {
            return target.Buffs.Find(b => b.Caster().IsMe && b.IsValid() && b.DisplayName == "KalistaExpungeMarker");
        }

        public static bool HasUndyingBuff(this AIHeroClient target)
        {
            // Various buffs
            if (target.Buffs.Any(
                b => b.IsValid() &&
                     (b.DisplayName == "Chrono Shift" /* Zilean R */||
                      b.DisplayName == "JudicatorIntervention" /* Kayle R */||
                      b.DisplayName == "Undying Rage" /* Tryndamere R */)))
            {
                return true;
            }

            // Poppy R
            if (target.ChampionName == "Poppy")
            {
                if (EntityManager.Heroes.Allies.Any(o => !o.IsMe && o.Buffs.Any(b => b.Caster().NetworkId == target.NetworkId && b.IsValid() && b.DisplayName == "PoppyDITarget")))
                {
                    return true;
                }
            }

            return target.IsInvulnerable;
        }

        public static bool HasSpellShield(this AIHeroClient target)
        {
            // Various spellshields
            return target.HasBuffOfType(BuffType.SpellShield) || target.HasBuffOfType(BuffType.SpellImmunity);
        }

        public static List<T> MakeUnique<T>(this List<T> list) where T : Obj_AI_Base, new()
        {
            var uniqueList = new List<T>();

            foreach (var entry in list.Where(entry => uniqueList.All(e => e.NetworkId != entry.NetworkId)))
            {
                uniqueList.Add(entry);
            }

            list.Clear();
            list.AddRange(uniqueList);

            return list;
        }

        public static float TotalShieldHealth(this Obj_AI_Base target)
        {
            return target.Health + target.AllShield + target.AttackShield + target.MagicShield;
        }

        // TODO: finn0x please
        public static float HealthPercent(this Obj_AI_Base target)
        {
            return (target.Health / target.MaxHealth) * 100;
        }

        // TODO: finn0x please
        public static float ManaPercent(this Obj_AI_Base target)
        {
            return (target.Mana / target.MaxMana) * 100;
        }

        // TODO: finn0x please
        public static Obj_AI_Base Caster(this BuffInstance buffInstance)
        {
            var caster = EntityManager.Heroes.AllHeroes.FirstOrDefault(o => o.Name == buffInstance.SourceName);
            return caster ?? Player.Instance;
        }
    }
}
