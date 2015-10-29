using EloBuddy;
using EloBuddy.SDK;
using Settings = Xerath.Config.Modes.Harass;

namespace Xerath.Modes
{
    public sealed class Harass : ModeBase
    {
        public override bool ShouldBeExecuted()
        {
            return Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Harass);
        }

        public override void Execute()
        {
            // Q is already charging, ignore mana check
            if (Q.IsEnabledAndReady(Orbwalker.ActiveModes.Harass) && Q.IsCharging)
            {
                var target = TargetSelector.GetTarget(Q.MaximumRange, DamageType.Magical);
                if (target != null)
                {
                    var prediction = Q.GetPrediction(target);
                    if (prediction.HitChance >= Q.MinimumHitChance)
                    {
                        if (Q.IsFullyCharged)
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

            // Check mana
            if (Settings.ManaUsage > Player.ManaPercent)
            {
                return;
            }

            if (W.IsEnabledAndReady(Orbwalker.ActiveModes.Harass))
            {
                if (W.CastOnBestTarget())
                {
                    return;
                }
            }

            if (E.IsEnabledAndReady(Orbwalker.ActiveModes.Harass))
            {
                if (E.CastOnBestTarget())
                {
                    return;
                }
            }

            // Q chargeup
            if (Q.IsEnabledAndReady(Orbwalker.ActiveModes.Harass) && !Q.IsCharging)
            {
                var target = TargetSelector.GetTarget(Q.MaximumRange, DamageType.Magical);
                if (target != null)
                {
                    var prediction = Q.GetPrediction(target);
                    if (prediction.HitChance >= Q.MinimumHitChance)
                    {
                        Q.StartCharging();
                    }
                }
            }
        }
    }
}
