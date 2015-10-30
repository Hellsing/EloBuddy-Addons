using System;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Enumerations;
using SharpDX;
using Settings = Gnaaar.Config.Modes.Combo;

// ReSharper disable CompareOfFloatsByEqualityOperator

namespace Gnaaar.Modes
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
            // Item usage (General)
            if (Settings.UseItems)
            {
                var itemTarget = Gnaaar.AfterAttackTarget as AIHeroClient;
                if (Gnaaar.IsAfterAttack && itemTarget != null)
                {
                    // All in Kappa
                    if (Player.IsMegaGnar())
                    {
                        ItemManager.UseBotrk(itemTarget);
                    }
                    ItemManager.UseRanduin(itemTarget);
                    ItemManager.UseYoumuu(itemTarget);
                }
            }

            // Ignite
            if (Gnaaar.HasIgnite && Settings.UseIgnite)
            {
                var target = TargetSelector.GetTarget(600, DamageType.True);
                if (target != null && Player.GetSummonerSpellDamage(target, DamageLibrary.SummonerSpells.Ignite) > target.Health)
                {
                    Player.Spellbook.CastSpell(Player.GetSpellSlotFromName("SummonerDot"), target);
                }
            }

            // Mini
            if (Player.IsMiniGnar())
            {
                // Q usage
                if (Q.IsEnabledAndReady(Orbwalker.ActiveModes.Combo) && !Player.IsAboutToTransform())
                {
                    var target = Q.GetTarget();
                    if (target != null)
                    {
                        var prediction = Q.GetPrediction(target);

                        switch (prediction.HitChance)
                        {
                            case HitChance.High:
                            case HitChance.Immobile:

                                // Regular Q cast
                                if (Q.Cast(prediction.CastPosition))
                                {
                                    return;
                                }
                                break;

                            case HitChance.Collision:

                                // Special case for colliding enemies
                                var colliding = prediction.CollisionObjects.OrderBy(o => o.Distance(Player, true)).ToList();
                                if (colliding.Count > 0)
                                {
                                    // First colliding target is < 100 units away from our main target
                                    if (colliding[0].IsInRange(target, 100))
                                    {
                                        if (Q.Cast(prediction.CastPosition))
                                        {
                                            return;
                                        }
                                    }
                                }
                                break;
                        }
                    }
                }

                // E usage (only when transforming into Mega Gnar)
                if (E.IsEnabledAndReady(Orbwalker.ActiveModes.Combo) && Player.IsAboutToTransform())
                {
                    var target = E.GetTarget(E.Width / 2f);
                    if (target != null)
                    {
                        var prediction = E.GetPrediction(target);

                        if (prediction.HitChance >= HitChance.High)
                        {
                            // Get the landing point of our E
                            var arrivalPoint = Player.ServerPosition.Extend(prediction.CastPosition, Player.ServerPosition.Distance(prediction.CastPosition) + E.Range);

                            // If we will land in the tower attack range of 775, don't continue
                            if (!ObjectManager.Get<Obj_AI_Turret>().Any(t => t.Team != Player.Team && !t.IsDead && t.Distance(arrivalPoint, true) < 775 * 775))
                            {
                                // Arrival point won't be in the turret range, cast it
                                if (E.Cast(prediction.CastPosition))
                                {
                                    return;
                                }
                            }
                        }
                    }
                }
            }
            // Mega
            else
            {
                // Item usage (Hydra/Tiamat)
                if (Gnaaar.IsAfterAttack && Settings.UseItems && ItemManager.UseHydra(Gnaaar.AfterAttackTarget as Obj_AI_Base))
                {
                    return;
                }

                // R usage

                #region Ult calculations

                if (R.IsEnabledAndReady(Orbwalker.ActiveModes.Combo) && !SpellManager.HasCastedStun)
                {
                    var target = R.GetTarget();
                    if (target != null && target.GetStunDuration() < R.CastDelay)
                    {
                        var prediction = Prediction.Position.PredictUnitPosition(target, R.CastDelay);
                        if (R.IsInRange(prediction.To3DWorld()))
                        {
                            // 12 angle checks for casting, prefer to Player direction
                            var direction = (Player.ServerPosition.To2D() - prediction).Normalized();
                            const float maxAngle = 180f;
                            const float step = maxAngle / 6f;
                            var currentAngle = 0f;
                            var currentStep = 0f;
                            while (true)
                            {
                                // Validate the counter, break if no valid spot was found in previous loops
                                if (currentStep > maxAngle && currentAngle < 0)
                                {
                                    break;
                                }

                                // Check next angle
                                if ((currentAngle == 0 || currentAngle < 0) && currentStep != 0)
                                {
                                    currentAngle = (currentStep) * (float) Math.PI / 180;
                                    currentStep += step;
                                }
                                else if (currentAngle > 0)
                                {
                                    currentAngle = -currentAngle;
                                }

                                Vector2 checkPoint;

                                // One time only check for direct line of sight without rotating
                                if (currentStep == 0)
                                {
                                    currentStep = step;
                                    checkPoint = prediction + 500 * direction;
                                }
                                // Rotated check
                                else
                                {
                                    checkPoint = prediction + 500 * direction.Rotated(currentAngle);
                                }

                                // Check for a wall between the checkPoint and the target position
                                if (prediction.GetFirstWallPoint(checkPoint).HasValue)
                                {
                                    // Cast ult into the direction where the wall is located
                                    if (R.Cast((Player.Position.To2D() + 500 * (checkPoint - prediction).Normalized()).To3DWorld()))
                                    {
                                        return;
                                    }
                                }
                            }
                        }
                    }
                }

                #endregion

                // W usage
                if (W.IsEnabledAndReady(Orbwalker.ActiveModes.Combo) && !SpellManager.HasCastedStun)
                {
                    var target = W.GetTarget();
                    if (target != null && target.GetStunDuration() < W.CastDelay)
                    {
                        // Only cast if target is not already stunned to make the longest chain possible
                        W.Cast(target);
                    }
                }

                // E usasge
                if (E.IsEnabledAndReady(Orbwalker.ActiveModes.Combo))
                {
                    var target = E.GetTarget(E.Width / 2f);
                    if (target != null)
                    {
                        // Cast without much logic
                        E.Cast(target);
                    }
                }

                // Q usage
                if (Q.IsEnabledAndReady(Orbwalker.ActiveModes.Combo))
                {
                    var target = Q.GetTarget();
                    if (target != null)
                    {
                        // Cast without much logic
                        Q.Cast(target);
                    }
                }
            }
        }
    }
}
