using EloBuddy;
using EloBuddy.SDK;
using Settings = Hellsing.Kalista.Config.Modes.Harass;

namespace Hellsing.Kalista.Modes
{
    public class Harass : ModeBase
    {
        public override bool ShouldBeExecuted()
        {
            return Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Harass);
        }

        public override void Execute()
        {
            // Mana check
            if (Player.ManaPercent < Settings.MinMana)
            {
                return;
            }

            if (Settings.UseQ && Q.IsReady())
            {
                var target = TargetSelector.GetTarget(Q.Range, DamageType.Physical);
                if (target != null)
                {
                    Q.Cast(target);
                }
            }
        }
    }
}
