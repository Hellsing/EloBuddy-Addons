using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using SharpDX;
using Settings = Kindred.Config.Modes.Combo;

namespace Kindred.Modes
{
    public sealed class Combo : ModeBase
    {
        public bool QSent { get; set; }

        public override bool ShouldBeExecuted()
        {
            return Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo);
        }

        public override void Execute()
        {
            var availableSpells = SpellManager.Spells.Where(spell => spell.IsReady() && spell.IsEnabled(Orbwalker.ActiveModes.Combo)).ToArray();
            if (availableSpells.Length > 0)
            {
                var longestRange = availableSpells[0].Range + (availableSpells[0].Slot == SpellSlot.Q ? SpellManager.QAcquisitionRange : 0);

                var target = Kindred.GetTarget(longestRange);
                if (target != null)
                {
                    foreach (var spell in availableSpells)
                    {
                        switch (spell.Slot)
                        {
                            case SpellSlot.Q:
                                if (target.IsInRange(Player, Q.Range + SpellManager.QAcquisitionRange))
                                {
                                    // Acquiring the dash point
                                    var dashPoint = Player.ServerPosition.Extend(target, Q.Range - Kindred.Random.NextFloat(0, Q.Range * 0.05f));

                                    // Acquire a new point if we dash into the auto attack range and don't use W at all
                                    if ((!Settings.UseW || !W.IsReady(300) || !Settings.GaploseQBeforeW || W.State == SpellState.Surpressed) &&
                                        !target.IsInAutoAttackRange(Player) && target.IsInRange(dashPoint, target.GetAutoAttackRange(Player)))
                                    {
                                        dashPoint = target.ServerPosition.Extend(Player, target.GetAutoAttackRange(Player) + 15);
                                    }

                                    // Cast Q
                                    if (Q.Cast(dashPoint.To3DWorld()))
                                    {
                                        QSent = true;
                                        Core.DelayAction(() => QSent = false, 300);
                                        return;
                                    }
                                }
                                break;

                            case SpellSlot.W:
                                if (Player.IsInRange(target, Settings.TriggerDistanceW))
                                {
                                    if (Settings.GaploseQBeforeW)
                                    {
                                        // Cast Q first or wait till it's done casting
                                        if (Q.IsReady() || QSent)
                                        {
                                            return;
                                        }
                                    }

                                    // Cast W
                                    if (W.Cast())
                                    {
                                        return;
                                    }
                                }
                                break;

                            case SpellSlot.E:
                                if (Player.IsInRange(target, E.Range))
                                {
                                    if (Settings.UseAdvancedE)
                                    {
                                        // Check if the player can land all 3 hits in theory
                                        if (target.Health / Player.GetAutoAttackDamage(target) < 3)
                                        {
                                            // Not worth it
                                            return;
                                        }
                                    }

                                    // Cast E
                                    if (E.Cast(target))
                                    {
                                        return;
                                    }
                                }
                                break;
                        }
                    }
                }
            }
        }
    }
}
