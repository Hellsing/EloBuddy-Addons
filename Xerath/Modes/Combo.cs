using EloBuddy;
using EloBuddy.SDK;
using Settings = Xerath.Config.Modes.Combo;

namespace Xerath.Modes
{
    public sealed class Combo : ModeBase
    {
        public bool QSent { get; set; }

        public override bool ShouldBeExecuted()
        {
            return Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo);
        }

        public override void Execute()
        {
            // Validate that Q is not charging
            if (!Q.IsCharging)
            {
                if (W.IsEnabledAndReady(Orbwalker.ActiveModes.Combo))
                {
                    if (W.CastOnBestTarget())
                    {
                        return;
                    }
                }

                if (E.IsEnabledAndReady(Orbwalker.ActiveModes.Combo))
                {
                    var target = E.GetTarget();
                    if (target != null && (target.GetStunDuration() == 0 || target.GetStunDuration() < (Player.ServerPosition.Distance(target.ServerPosition) / E.Speed + E.CastDelay / 1000f) * 1000))
                    {
                        if (E.Cast(target))
                        {
                            return;
                        }
                    }
                }
            }

            if (Q.IsEnabledAndReady(Orbwalker.ActiveModes.Combo))
            {
                var target = TargetSelector.GetTarget(Q.MaximumRange, DamageType.Magical);
                if (target != null)
                {
                    var prediction = Q.GetPrediction(target);
                    if (prediction.HitChance >= Q.MinimumHitChance)
                    {
                        if (!Q.IsCharging)
                        {
                            Q.StartCharging();
                            return;
                        }
                        if (Q.Range == Q.MaximumRange)
                        {
                            if (Q.Cast(target))
                            {
                                return;
                            }
                        }
                        else
                        {
                            if (Player.IsInRange(prediction.UnitPosition + Settings.ExtraRangeQ * (prediction.UnitPosition - Player.ServerPosition).Normalized(), Q.Range))
                            {
                                if (Q.Cast(prediction.CastPosition))
                                {
                                    return;
                                }
                            }
                        }
                    }
                }
            }

            // Validate that Q is not charging
            if (Q.IsCharging)
            {
                return;
            }

            if (R.IsEnabledAndReady(Orbwalker.ActiveModes.Combo) && !SpellManager.IsCastingUlt)
            {
                var target = R.GetTarget();
                if (target != null && R.GetRealDamage(target) * 3 > target.Health)
                {
                    // Only activate ult if the target can die from it
                    R.Cast();
                }
            }
        }
    }
}
