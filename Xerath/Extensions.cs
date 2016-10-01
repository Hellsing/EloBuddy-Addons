using System;
using System.Collections.Generic;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using SharpDX;

namespace Xerath
{
    public static class Extensions
    {
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
                if (EntityManager.Heroes.Allies.Any(o => !o.IsMe && o.Buffs.Any(b => b.Caster.NetworkId == target.NetworkId && b.IsValid() && b.DisplayName == "PoppyDITarget")))
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

        public static float TotalShieldHealth(this Obj_AI_Base target)
        {
            return target.Health + target.AllShield + target.AttackShield + target.MagicShield;
        }

        public static int GetStunDuration(this Obj_AI_Base target)
        {
            return (int) (target.Buffs.Where(b => b.IsActive && Game.Time < b.EndTime &&
                                                  (b.Type == BuffType.Charm ||
                                                   b.Type == BuffType.Knockback ||
                                                   b.Type == BuffType.Stun ||
                                                   b.Type == BuffType.Suppression ||
                                                   b.Type == BuffType.Snare)).Aggregate(0f, (current, buff) => Math.Max(current, buff.EndTime)) - Game.Time) * 1000;
        }

        public static bool IsPassiveReady(this AIHeroClient target)
        {
            return target.IsMe && target.HasBuff("XerathAscended2OnHit");
        }

        public static EntityManager.MinionsAndMonsters.FarmLocation GetCircularFarmLocation(this Spell.Skillshot spell, IEnumerable<Obj_AI_Minion> entities)
        {
            var minions = entities.Cast<Obj_AI_Base>().ToArray();
            var hitNumber = 0;
            var castPosition = Vector2.Zero;
            var predictionResultArray =
                minions.Select(o => Prediction.Position.PredictCircularMissile(o, spell.Range, spell.Radius, spell.CastDelay, spell.Speed))
                    .Where(o => o.CastPosition.IsInRange(spell.SourcePosition.HasValue ? spell.SourcePosition.Value.To2D() : Player.Instance.Position.To2D(), spell.Range + spell.Radius))
                    .ToArray();

            foreach (var predictionResult in predictionResultArray)
            {
                var pos = predictionResult;
                var currentHitNumber = predictionResultArray.Count(o => o.CastPosition.IsInRange(pos.CastPosition, spell.Radius));
                if (currentHitNumber >= hitNumber)
                {
                    castPosition = pos.CastPosition.To2D();
                    hitNumber = currentHitNumber;
                }
            }

            return new EntityManager.MinionsAndMonsters.FarmLocation
            {
                CastPosition = castPosition.To3DWorld(),
                HitNumber = hitNumber
            };
        }
    }
}
