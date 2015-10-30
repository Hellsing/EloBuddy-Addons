using System.Linq;
using EloBuddy.SDK;
using EloBuddy.SDK.Enumerations;
using Settings = Gnaaar.Config.Modes.Harass;

namespace Gnaaar.Modes
{
    public sealed class Harass : ModeBase
    {
        public override bool ShouldBeExecuted()
        {
            return Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Harass);
        }

        public override void Execute()
        {
            // Q usage
            if (Q.IsEnabledAndReady(Orbwalker.ActiveModes.Harass) && (Player.IsMiniGnar() && (!Player.IsAboutToTransform() || Settings.UseQMega)))
            {
                var target = Q.GetTarget();
                if (target != null)
                {
                    var prediction = Q.GetPrediction(target);

                    switch (prediction.HitChance)
                    {
                        case HitChance.High:
                        case HitChance.Immobile:

                            // Regular Q cast
                            if (Q.Cast(prediction.CastPosition))
                            {
                                return;
                            }
                            break;

                        case HitChance.Collision:

                            // Special case for colliding enemies
                            var colliding = prediction.CollisionObjects.OrderBy(o => o.Distance(Player, true)).ToList();
                            if (colliding.Count > 0)
                            {
                                // First colliding target is < 100 units away from our main target
                                if (colliding[0].Distance(target, true) < 10000)
                                {
                                    if (Q.Cast(prediction.CastPosition))
                                    {
                                        return;
                                    }
                                }
                            }
                            break;
                    }
                }
            }

            // Mega
            if (Player.IsMegaGnar())
            {
                if (W.IsEnabledAndReady(Orbwalker.ActiveModes.Harass))
                {
                    var target = W.GetTarget();
                    if (target != null)
                    {
                        W.Cast(target);
                    }
                }
            }
        }
    }
}
