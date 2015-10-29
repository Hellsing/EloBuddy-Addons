using System.Linq;
using EloBuddy.SDK;
using Settings = Hellsing.Kalista.Config.Modes.LaneClear;

namespace Hellsing.Kalista.Modes
{
    public class LaneClear : ModeBase
    {
        public override bool ShouldBeExecuted()
        {
            return Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LaneClear);
        }

        public override void Execute()
        {
            if (Player.ManaPercent < Settings.MinMana)
            {
                return;
            }

            // Precheck
            if (!(Settings.UseQ && Q.IsReady()) && !(Settings.UseE && E.IsReady()))
            {
                return;
            }

            // Minions around
            var minions = EntityManager.MinionsAndMonsters.GetLaneMinions(EntityManager.UnitTeam.Enemy, Player.ServerPosition, Q.Range, false);

            // TODO: Readd Q logic once Collision is added

            #region E usage

            if (Settings.UseE && E.IsReady())
            {
                // Get minions in E range
                var minionsInRange = minions.Where(m => E.IsInRange(m)).ToArray();

                // Validate available minions
                if (minionsInRange.Length >= Settings.MinNumberE)
                {
                    // Check if enough minions die with E
                    var killableNum = 0;
                    foreach (var minion in minionsInRange.Where(minion => minion.IsRendKillable()))
                    {
                        // Increase kill number
                        killableNum++;

                        // Cast on condition met
                        if (killableNum >= Settings.MinNumberE)
                        {
                            E.Cast();
                            break;
                        }
                    }
                }
            }

            #endregion
        }
    }
}
