using System.Linq;
using EloBuddy.SDK;

namespace Gnaaar.Modes
{
    public sealed class PermaActive : ModeBase
    {
        public override bool ShouldBeExecuted()
        {
            return true;
        }

        public override void Execute()
        {
            // Face of the Mountain
            if (Config.Items.UseFaceOfTheMountain && ItemManager.FaceMountain.IsReady())
            {
                foreach (var ally in EntityManager.Heroes.Allies.Where(o => o.IsValidTarget(ItemManager.FaceMountain.Range))
                    .Where(ally => ally.HealthPercent < 15 && ally.CountEnemiesInRange(700) > 0))
                {
                    ItemManager.FaceMountain.Cast(ally);
                    break;
                }
            }
        }
    }
}
