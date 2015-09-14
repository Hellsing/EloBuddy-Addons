using System.Linq;
using EloBuddy.SDK;
using Settings = Hellsing.Kalista.Config.Modes.JungleClear;

namespace Hellsing.Kalista.Modes
{
    public class JungleClear : ModeBase
    {
        public override bool ShouldBeExecuted()
        {
            return Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.JungleClear);
        }

        public override void Execute()
        {
            if (!Settings.UseE || !E.IsReady())
            {
                return;
            }

            // Get a jungle mob that can die with E
            if (EntityManager.GetJungleMonsters(Player.ServerPosition.To2D(), E.Range).Any(m => m.IsRendKillable()))
            {
                E.Cast();
            }
        }
    }
}
