using System.Linq;
using EloBuddy.SDK;
using Settings = Xerath.Config.Modes.JungleClear;

namespace Xerath.Modes
{
    public sealed class JungleClear : ModeBase
    {
        public override bool ShouldBeExecuted()
        {
            return Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.JungleClear);
        }

        public override void Execute()
        {
            // Validate Q, W and E are ready
            if (!Q.IsEnabledAndReady(Orbwalker.ActiveModes.JungleClear) && !W.IsEnabledAndReady(Orbwalker.ActiveModes.JungleClear) && !E.IsEnabledAndReady(Orbwalker.ActiveModes.JungleClear))
            {
                return;
            }

            // Get the minions around
            var minions = EntityManager.MinionsAndMonsters.GetJungleMonsters(Player.ServerPosition, W.Range, false).ToArray();
            if (minions.Length == 0)
            {
                return;
            }

            if (Q.IsEnabledAndReady(Orbwalker.ActiveModes.JungleClear))
            {
                var farmLocation = EntityManager.MinionsAndMonsters.GetLineFarmLocation(minions, Q.Width, (int) (Q.IsCharging ? Q.Range : Q.MaximumRange));
                if (farmLocation.HitNumber > 0)
                {
                    if (!Q.IsCharging)
                    {
                        Q.StartCharging();
                        return;
                    }
                    if (Q.Cast(farmLocation.CastPosition))
                    {
                        return;
                    }
                }
            }

            // Validate that Q is not charging
            if (Q.IsCharging)
            {
                return;
            }

            if (W.IsEnabledAndReady(Orbwalker.ActiveModes.JungleClear))
            {
                var farmLocation = EntityManager.MinionsAndMonsters.GetCircularFarmLocation(minions, W.Width, (int) W.Range);
                if (farmLocation.HitNumber > 0)
                {
                    if (W.Cast(farmLocation.CastPosition))
                    {
                        return;
                    }
                }
            }

            if (E.IsEnabledAndReady(Orbwalker.ActiveModes.JungleClear))
            {
                E.Cast(minions[0]);
            }
        }
    }
}
