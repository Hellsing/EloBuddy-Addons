using System;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;

namespace Xerath.Modes
{
    public sealed class PermaActive : ModeBase
    {
        private AIHeroClient _lastUltTarget;
        private bool _targetWillDie;
        //private int _orbUsedTime; TODO
        private int _lastAltert;

        public override bool ShouldBeExecuted()
        {
            return true;
        }

        public override void Execute()
        {
            // Disable movement while ulting
            Orbwalker.DisableMovement = SpellManager.IsCastingUlt;

            // Alerter for ultimate
            if (Config.Misc.Alerter && (SpellManager.IsCastingUlt || R.IsReady()) && Environment.TickCount - _lastAltert > 5000)
            {
                // Get targets that can die with R
                var killableTargets = EntityManager.Heroes.Enemies
                    .Where(h => h.IsValidTarget(R.Range) && h.Health < (SpellManager.IsCastingUlt ? SpellManager.ChargesRemaining : 3) * R.GetRealDamage(h))
                    .OrderByDescending(h => R.GetRealDamage(h)).ToArray();

                if (killableTargets.Length > 0)
                {
                    _lastAltert = Environment.TickCount;
                    var time = TimeSpan.FromSeconds(Game.Time);
                    Chat.Print(string.Format("[{0}:{1:D2}] Targets killable: {2}", Math.Floor(time.TotalMinutes), time.Seconds, string.Join(", ", killableTargets.Select(t => t.ChampionName))));
                }
            }

            // Ult handling
            if (SpellManager.IsCastingUlt && Config.Ultimate.Enabled)
            {
                switch (Config.Ultimate.CurrentMode)
                {
                        #region Smart targetting & Obviously scripting & On key press (auto)

                        // Smart targetting
                    case 0:
                    // Obviously scripting
                    case 1:
                    // On key press (auto)
                    case 3:

                        // Only for tap key
                        if (Config.Ultimate.CurrentMode == 3 && !SpellManager.TapKeyPressed)
                        {
                            break;
                        }

                        // Get first time target
                        if (_lastUltTarget == null || SpellManager.ChargesRemaining == 3)
                        {
                            var target = R.GetTarget();
                            if (target != null && R.Cast(target))
                            {
                                _lastUltTarget = target;
                                _targetWillDie = target.Health < R.GetRealDamage(target);
                            }
                        }
                        // Next target
                        else if (SpellManager.ChargesRemaining < 3)
                        {
                            // Shoot the same target again if in range
                            if ((!_targetWillDie || Environment.TickCount - SpellManager.LastChargeTime > R.CastDelay + 100) && _lastUltTarget.IsValidTarget(R.Range))
                            {
                                if (R.Cast(_lastUltTarget))
                                {
                                    _targetWillDie = _lastUltTarget.Health < R.GetRealDamage(_lastUltTarget);
                                }
                            }
                            // Target died or is out of range, shoot new target
                            else
                            {
                                /* TODO
                                // Check if last target is still alive
                                if (!_lastUltTarget.IsDead && ItemManager.UseRevealingOrb(_lastUltTarget.ServerPosition))
                                {
                                    _orbUsedTime = Environment.TickCount;
                                    break;
                                }

                                // Check if orb was used
                                if (Environment.TickCount - _orbUsedTime < 250)
                                    break;
                                */

                                // Get a new target
                                var target = R.GetTarget(_lastUltTarget);
                                if (target != null)
                                {
                                    // Only applies if smart targetting is enabled
                                    if (Config.Ultimate.CurrentMode == 0)
                                    {
                                        // Calculate smart target change time
                                        var waitTime = Math.Max(1500, target.Distance(SpellManager.LastChargePosition)) + R.CastDelay;
                                        if (Environment.TickCount - SpellManager.LastChargeTime + waitTime < 0)
                                        {
                                            break;
                                        }
                                    }

                                    if (R.Cast(target))
                                    {
                                        _lastUltTarget = target;
                                        _targetWillDie = target.Health < R.GetRealDamage(target);
                                    }
                                }
                            }
                        }

                        break;

                        #endregion

                        #region Near mouse & On key press (near mouse)

                        // Near mouse
                    case 2:
                    // On key press (near mouse)
                    case 4:

                        // Only for tap key
                        if (Config.Ultimate.CurrentMode == 4 && !SpellManager.TapKeyPressed)
                            break;

                        // Get all enemy heroes in a distance of 500 from the mouse
                        var targets = EntityManager.Heroes.Enemies.Where(h => h.IsValidTarget(R.Range) && h.IsInRange(Game.CursorPos, 500)).ToArray();
                        if (targets.Length > 0)
                        {
                            // Get a killable target
                            var killable = targets.Where(t => t.Health < R.GetRealDamage(t) * SpellManager.ChargesRemaining).OrderByDescending(t => R.GetRealDamage(t)).FirstOrDefault();
                            if (killable != null)
                            {
                                // Shoot on the killable target
                                R.Cast(killable);
                            }
                            else
                            {
                                // Get the best target out of the found targets
                                var target = targets.OrderByDescending(t => R.GetRealDamage(t)).FirstOrDefault();

                                // Shoot
                                R.Cast(target);
                            }
                        }

                        break;

                        #endregion
                }
            }
        }
    }
}
