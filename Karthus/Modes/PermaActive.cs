using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Menu.Values;

namespace Karthus.Modes
{
    public sealed class PermaActive : ModeBase
    {
        private CheckBox UnkillableE { get; set; }

        public PermaActive(Karthus instance) : base(instance)
        {
            // Setup menu
            Menu.AddGroupLabel("Spell usage");
            UnkillableE = Menu.Add("unkillableE", new CheckBox("Cast E if minion is not lasthittable"));
            Menu.AddLabel("Note: This will only trigger when in LaneClear or JungleClear mode!");

            // Listen to required events
            Orbwalker.OnUnkillableMinion += OnUnkillableMinion;
        }

        private void OnUnkillableMinion(Obj_AI_Base target, Orbwalker.UnkillableMinionArgs args)
        {
            if (UnkillableE.CurrentValue && (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LaneClear) || Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.JungleClear)))
            {
                // Check if target is in E range and killable with E
                if (E.IsReady() && E.IsInRange(target) && !IsDefileActive && target.TotalShieldHealth() < Player.Instance.GetSpellDamage(target, E.Slot))
                {
                    // Cast E
                    CastDefilePulse();
                }
            }
        }

        public override bool ShouldBeExecuted(Orbwalker.ActiveModes activeModes)
        {
            return true;
        }

        public override bool Execute()
        {
            return false;
        }
    }
}
