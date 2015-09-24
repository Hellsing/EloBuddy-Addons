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

                if (Settings.UseKillsteal && HeroManager.Enemies.Any(h => h.IsValidTarget(E.Range) && h.IsRendKillable()) && E.Cast())
                {
                    return;
                }

                #endregion

                #region E on big mobs

                if (Settings.UseEBig && ObjectManager.Get<Obj_AI_Base>().Any(m =>
                {
                    if (!m.IsAlly && m.IsValidTarget(E.Range) && m.HasRendBuff())
                    {
                        var skinName = m.BaseSkinName.ToLower();
                        return (skinName.Contains("siege") || skinName.Contains("super") || skinName.Contains("dragon") || skinName.Contains("baron")) &&
                               m.IsRendKillable();
                    }
                    return false;
                }) && E.Cast())
                {
                    return;
                }

                #endregion

                #region E combo (harass plus)

                if (Settings.UseHarassPlus)
                {
                    if (HeroManager.Enemies.Any(o => E.IsInRange(o) && o.HasRendBuff()) &&
                        ObjectManager.Get<Obj_AI_Base>().Any(o => {
                            if (!o.IsAlly && o.IsValidTarget(E.Range) && o.HasRendBuff())
                            {
                                return o.IsRendKillable();
                            }
                            return false;
                        }
                        ) &&
                        E.Cast())
                    {
                        return;
                    }
                }

                #endregion

                #region E before death

                if (Player.HealthPercent < Settings.AutoEBelowHealth && E.Cast())
                {
                    return;
                }

                #endregion
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
