using System;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using Settings = Hellsing.Kalista.Config.Misc;

namespace Hellsing.Kalista.Modes
{
    public class PermaActive : ModeBase
    {
        public PermaActive()
        {
            // Listen to required events
            Orbwalker.OnPostAttack += OnPostAttack;
            Orbwalker.OnUnkillableMinion += OnUnkillableMinion;
        }

        public override bool ShouldBeExecuted()
        {
            return true;
        }

        public override void Execute()
        {
            // Clear the forced target
            Orbwalker.ForcedTarget = null;

            if (E.IsReady())
            {
                #region Killsteal

                if (Settings.UseKillsteal && Cache.RendHeroes.Any(o => o.IsRendKillable()) && E.Cast())
                {
                    return;
                }

                #endregion

                #region E on big mobs

                if (Settings.UseEBig)
                {
                    if (Cache.RendMinions.Where(o => o.IsRendKillable()).Any(m =>
                    {
                        var skinName = m.BaseSkinName.ToLower();
                        return (skinName.Contains("siege") ||
                                skinName.Contains("super") ||
                                skinName.Contains("dragon") ||
                                skinName.Contains("baron") ||
                                skinName.Contains("spiderboss"));
                    }) && E.Cast())
                    {
                        return;
                    }
                }

                #endregion

                #region E combo (harass plus)

                if (Settings.UseHarassPlus)
                {
                    if (Cache.RendHeroes.Any(o => E.IsInRange(o)) && Cache.RendMinions.Any(o => E.IsInRange(o) && o.IsRendKillable()) && E.Cast())
                    {
                        return;
                    }
                }

                #endregion

                #region E before death

                if (Player.HealthPercent < Settings.AutoEBelowHealth && Cache.RendHeroes.Any(o => E.IsInRange(o)) && E.Cast())
                {
                    return;
                }

                #endregion
            }
        }

        private void OnPostAttack(AttackableUnit target, EventArgs args)
        {
            if (Config.Modes.Combo.UseQAA &&
                Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo) &&
                Player.ManaPercent > Config.Modes.Combo.ManaQ &&
                Q.IsReady())
            {
                var hero = target as AIHeroClient;
                if (hero != null && Player.GetAutoAttackDamage(hero) < hero.Health + hero.AllShield + hero.AttackShield)
                {
                    // Cast Q after auto attack (combo setting)
                    Q.Cast(hero);
                }
            }
        }

        private void OnUnkillableMinion(Obj_AI_Base target, Orbwalker.UnkillableMinionArgs args)
        {
            if (Settings.SecureMinionKillsE && E.IsReady() && target.IsRendKillable())
            {
                // Cast since it's killable with E
                SpellManager.E.Cast();
            }
        }
    }
}
