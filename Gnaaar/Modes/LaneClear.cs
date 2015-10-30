using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using Settings = Gnaaar.Config.Modes.LaneClear;

namespace Gnaaar.Modes
{
    public sealed class LaneClear : ModeBase
    {
        public override bool ShouldBeExecuted()
        {
            return Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LaneClear);
        }

        public override void Execute()
        {
            // Mini
            if (Player.IsMiniGnar())
            {
                // Q usage
                if (Q.IsEnabledAndReady(Orbwalker.ActiveModes.LaneClear) && (!Player.IsAboutToTransform() || Settings.UseQMega))
                {
                    // Get minions in Q range which could die with Q but not with AA
                    var minions = EntityManager.MinionsAndMonsters.GetLaneMinions(EntityManager.UnitTeam.Enemy, Player.ServerPosition, Q.Range).Where(m =>
                        m.Health > Player.GetAutoAttackDamage(m) / 2 && m.Health < Q.GetRealDamage(m));
                    // Get farm location
                    var position = Q.GetFarmLocation(EntityManager.UnitTeam.Enemy, EntityManager.MinionsAndMonsters.EntityType.Minion, minions);
                    if (position.HasValue)
                    {
                        Q.Cast(position.Value.CastPosition);
                    }
                }
            }
            // Mega (this is just wasting spells, I disable every spell in here for myself :P)
            else
            {
                // Item usage
                if (Gnaaar.IsAfterAttack && Settings.UseItems && ItemManager.UseHydra(Gnaaar.AfterAttackTarget as Obj_AI_Base))
                {
                    return;
                }

                // Q usage
                if (Q.IsEnabledAndReady(Orbwalker.ActiveModes.LaneClear))
                {
                    // Get minions in Q range which could die with Q but not with AA
                    var minions = EntityManager.MinionsAndMonsters.GetLaneMinions(EntityManager.UnitTeam.Enemy, Player.ServerPosition, Q.Range).Where(m =>
                        m.Health > Player.GetAutoAttackDamage(m) / 2 && m.Health < Q.GetRealDamage(m));
                    // Get farm location
                    var position = Q.GetFarmLocation(EntityManager.UnitTeam.Enemy, EntityManager.MinionsAndMonsters.EntityType.Minion, minions);
                    if (position.HasValue)
                    {
                        if (Q.Cast(position.Value.CastPosition))
                        {
                            return;
                        }
                    }
                }

                // W usage
                if (W.IsEnabledAndReady(Orbwalker.ActiveModes.LaneClear))
                {
                    // Get farm location
                    var position = W.GetFarmLocation();
                    if (position.HasValue)
                    {
                        if (W.Cast(position.Value.CastPosition))
                        {
                            return;
                        }
                    }
                }

                // E usage
                if (E.IsEnabledAndReady(Orbwalker.ActiveModes.LaneClear))
                {
                    // Get farm location
                    var position = E.GetFarmLocation();
                    if (position.HasValue)
                    {
                        E.Cast(position.Value.CastPosition);
                    }
                }
            }
        }
    }
}
