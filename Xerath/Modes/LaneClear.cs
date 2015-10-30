using System.Linq;
using EloBuddy.SDK;
using Settings = Xerath.Config.Modes.LaneClear;

namespace Xerath.Modes
{
    public sealed class LaneClear : ModeBase
    {
        public override bool ShouldBeExecuted()
        {
            return Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LaneClear);
        }

        public override void Execute()
        {
            // Get the minions around
            var minions = EntityManager.MinionsAndMonsters.GetLaneMinions(EntityManager.UnitTeam.Enemy, Player.ServerPosition, Q.MaximumRange, false).ToArray();
            if (minions.Length == 0)
            {
                return;
            }

            // Q is charging, ignore mana check
            if (Q.IsEnabledAndReady(Orbwalker.ActiveModes.LaneClear) && Q.IsCharging)
            {
                // Check if we are on max range with the minions
                if (minions.Max(m => m.Distance(Player, true)) < Q.RangeSquared)
                {
                    if (Q.Cast(EntityManager.MinionsAndMonsters.GetLineFarmLocation(minions, Q.Width, (int) Q.Range).CastPosition))
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

            // Check mana
            if (Settings.ManaUsage > Player.ManaPercent)
                return;

            if (Q.IsEnabledAndReady(Orbwalker.ActiveModes.LaneClear))
            {
                if (minions.Length >= Settings.HitNumberQ)
                {
                    // Check if we would hit enough minions
                    if (EntityManager.MinionsAndMonsters.GetLineFarmLocation(minions, Q.Width, (int) Q.MaximumRange).HitNumber >= Settings.HitNumberQ)
                    {
                        // Start charging
                        Q.StartCharging();
                        return;
                    }
                }
            }

            if (W.IsEnabledAndReady(Orbwalker.ActiveModes.LaneClear))
            {
                if (minions.Length >= Settings.HitNumberW)
                {
                    var farmLocation = EntityManager.MinionsAndMonsters.GetCircularFarmLocation(minions, W.Width, (int) W.Range);
                    if (farmLocation.HitNumber >= Settings.HitNumberW)
                    {
                        if (W.Cast(farmLocation.CastPosition))
                        {
                            return;
                        }
                    }
                }
            }
        }
    }
}
