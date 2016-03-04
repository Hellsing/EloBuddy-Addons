using System.Collections.Generic;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Menu.Values;

namespace Karthus.Modes
{
    public sealed class Combo : ModeBase
    {
        private Dictionary<SpellSlot, CheckBox> SpellUsage { get; set; }

        public Combo(Karthus instance) : base(instance)
        {
            // Initialize properties
            SpellUsage = new Dictionary<SpellSlot, CheckBox>();

            // Setup menu
            Menu.AddGroupLabel("Spell usage");
            SpellUsage[SpellSlot.Q] = Menu.Add("Q", new CheckBox("Use Q"));
            SpellUsage[SpellSlot.W] = Menu.Add("W", new CheckBox("Use W"));
            SpellUsage[SpellSlot.E] = Menu.Add("E", new CheckBox("Use E"));
            SpellUsage[SpellSlot.R] = Menu.Add("R", new CheckBox("Use R", false));
        }

        public override bool ShouldBeExecuted(Orbwalker.ActiveModes activeModes)
        {
            if (activeModes.HasFlag(Orbwalker.ActiveModes.Combo))
            {
                return true;
            }
            var deadCombo = Instance.GetGlobal<CheckBox>("ComboWhileDead");
            return deadCombo != null && deadCombo.CurrentValue && Instance.IsDead;
        }

        public override bool Execute()
        {
            // Cast active spells
            return SpellUsage.Keys.Any(slot => SpellUsage[slot].CurrentValue && Player.GetSpell(slot).IsReady && Instance.SpellHandler.CastOnBestTarget(slot));
        }
    }
}
