using EloBuddy;
using EloBuddy.SDK;
using Settings = Gnaaar.Config.Modes.JungleClear;

namespace Gnaaar.Modes
{
    public sealed class JungleClear : ModeBase
    {
        public override bool ShouldBeExecuted()
        {
            return Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.JungleClear);
        }

        public override void Execute()
        {
            // Mini
            if (Player.IsMiniGnar())
            {
                // Q usage
                if (Q.IsEnabledAndReady(Orbwalker.ActiveModes.JungleClear) && (!Player.IsAboutToTransform() || Settings.UseQMega))
                {
                    // Get farm location
                    var position = Q.GetFarmLocation(EntityManager.UnitTeam.Both, EntityManager.MinionsAndMonsters.EntityType.Monster);
                    if (position.HasValue)
                    {
                        Q.Cast(position.Value.CastPosition);
                    }
                }
            }
            // Mega
            else
            {
                // Item usage
                if (Gnaaar.IsAfterAttack && Settings.UseItems && ItemManager.UseHydra(Gnaaar.AfterAttackTarget as Obj_AI_Base))
                {
                    return;
                }

                // Q usage
                if (Q.IsEnabledAndReady(Orbwalker.ActiveModes.JungleClear))
                {
                    // Get farm location
                    var position = Q.GetFarmLocation(EntityManager.UnitTeam.Both, EntityManager.MinionsAndMonsters.EntityType.Monster);
                    if (position.HasValue)
                    {
                        if (Q.Cast(position.Value.CastPosition))
                        {
                            return;
                        }
                    }
                }

                // W usage
                if (W.IsEnabledAndReady(Orbwalker.ActiveModes.JungleClear))
                {
                    // Get farm location
                    var position = W.GetFarmLocation(EntityManager.UnitTeam.Both, EntityManager.MinionsAndMonsters.EntityType.Monster);
                    if (position.HasValue)
                    {
                        if (W.Cast(position.Value.CastPosition))
                        {
                            return;
                        }
                    }
                }

                // E usage
                if (E.IsEnabledAndReady(Orbwalker.ActiveModes.JungleClear))
                {
                    // Get farm location
                    var position = E.GetFarmLocation(EntityManager.UnitTeam.Both, EntityManager.MinionsAndMonsters.EntityType.Monster);
                    if (position.HasValue)
                    {
                        E.Cast(position.Value.CastPosition);
                    }
                }
            }
        }
    }
}
