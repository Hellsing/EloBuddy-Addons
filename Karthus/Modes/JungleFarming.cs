using System.Collections.Generic;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Menu.Values;

namespace Karthus.Modes
{
    public sealed class JungleFarming : ModeBase
    {
        private Dictionary<SpellSlot, CheckBox> SpellUsage { get; set; }

        public JungleFarming(Karthus instance) : base(instance)
        {
            // Initialize properties
            SpellUsage = new Dictionary<SpellSlot, CheckBox>();

            // Setup menu
            Menu.AddGroupLabel("Spell usage");
            SpellUsage[SpellSlot.Q] = Menu.Add("Q", new CheckBox("Use Q"));
            SpellUsage[SpellSlot.W] = Menu.Add("W", new CheckBox("Use W"));
            SpellUsage[SpellSlot.E] = Menu.Add("E", new CheckBox("Use E"));
        }

        public override bool ShouldBeExecuted(Orbwalker.ActiveModes activeModes)
        {
            return activeModes.HasFlag(Orbwalker.ActiveModes.JungleClear);
        }

        public override bool Execute()
        {
            // Get surrounding jungle monsters
            var monsters = EntityManager.MinionsAndMonsters.GetJungleMonsters(Player.Instance.ServerPosition, Q.Range, false).ToArray();

            if (monsters.Length > 0)
            {
                // Check if we need to overwrite Defile turning off
                if (SpellUsage[E.Slot].CurrentValue)
                {
                    ShouldTurnOffDefile = false;
                }

                // Sort monsters by max health
                if (monsters.Length > 1)
                {
                    monsters = monsters.OrderByDescending(o => o.MaxHealth).ThenBy(o => o.Distance(Player.Instance, true)).ToArray();
                }

                // Get the monster to cast the spells on
                var target = monsters[0];

                foreach (var slot in SpellUsage.Keys.Where(slot => SpellUsage[slot].CurrentValue && Player.GetSpell(slot).State == SpellState.Ready))
                {
                    switch (slot)
                    {
                        case SpellSlot.Q:
                        case SpellSlot.W:

                            // Predict the position
                            var pos = target.ServerPosition;
                            if (target.MoveSpeed > 0)
                            {
                                pos = Prediction.Position.PredictUnitPosition(target, slot == SpellSlot.Q ? Q.CastDelay : W.CastDelay).To3DWorld();
                            }

                            // Check if position is in range
                            if (slot == SpellSlot.Q && !Q.IsInRange(pos))
                            {
                                break;
                            }

                            // Cast the spell directly on the target
                            if (Player.CastSpell(slot, pos))
                            {
                                // Q or W has been casted
                                return true;
                            }
                            break;

                        case SpellSlot.E:
                            if (!IsDefileActive && monsters.Any(o => E.IsInRange(o)) && E.Cast())
                            {
                                // Defile has been activated
                                return true;
                            }
                            break;
                    }
                }
            }
            else if (IsDefileActive)
            {
                // Deactivate Defile as there is no target around
                ShouldTurnOffDefile = true;
            }

            // Nothing was casted
            return false;
        }
    }
}
