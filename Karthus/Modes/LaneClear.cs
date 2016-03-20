using System.Collections.Generic;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Menu.Values;

namespace Karthus.Modes
{
    // ReSharper disable ConvertIfStatementToReturnStatement
    public sealed class LaneClear : ModeBase
    {
        private Dictionary<SpellSlot, CheckBox> SpellUsage { get; set; }

        private Slider KillcountE { get; set; }

        private Slider ManaUsage { get; set; }

        public LaneClear(Karthus instance) : base(instance)
        {
            // Initialize properties
            SpellUsage = new Dictionary<SpellSlot, CheckBox>();

            // Setup menu
            Menu.AddGroupLabel("Spell usage");
            //SpellUsage[SpellSlot.Q] = Menu.Add("Q", new CheckBox("Use Q"));
            SpellUsage[SpellSlot.E] = Menu.Add("E", new CheckBox("Use E"));

            Menu.AddSeparator();
            Menu.AddGroupLabel("Spell options");
            KillcountE = Menu.Add("countE", new Slider("Minimum kill count for E to trigger", 2, 1, 5));

            Menu.AddSeparator();
            Menu.AddGroupLabel("Mana usage");
            ManaUsage = Menu.Add("mana", new Slider("Only cast if mana is above {0}%", 50, 0, 99));
        }

        public override bool ShouldBeExecuted(Orbwalker.ActiveModes activeModes)
        {
            return activeModes.HasFlag(Orbwalker.ActiveModes.LaneClear);
        }

        public override bool Execute()
        {
            // Check for mana
            if (ManaUsage.CurrentValue > Player.Instance.ManaPercent)
            {
                if (IsDefileActive)
                {
                    ShouldTurnOffDefile = true;
                    return true;
                }
                return false;
            }

            // E usage
            if (SpellUsage[E.Slot].CurrentValue && E.IsReady() && !IsDefileActive)
            {
                // Get minions in E range
                var minions = EntityManager.MinionsAndMonsters.GetLaneMinions(radius: E.Range, addBoundingRadius: false).ToArray();
                if (minions.Length >= KillcountE.CurrentValue)
                {
                    var killable = minions.Count(minion => Player.Instance.GetSpellDamage(minion, E.Slot) > minion.TotalShieldHealth());
                    if (killable >= KillcountE.CurrentValue && CastDefilePulse())
                    {
                        // E was casted
                        return true;
                    }
                }
            }

            // Nothing was casted
            return false;
        }
    }
}
