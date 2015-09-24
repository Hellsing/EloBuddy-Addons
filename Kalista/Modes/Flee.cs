using System;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Events;
using SharpDX;
using Settings = Hellsing.Kalista.Config.Modes.Flee;
using EloBuddy.SDK.Rendering;

namespace Hellsing.Kalista.Modes
{
    public class Flee : ModeBase
    {
        private Vector3 Target { get; set; }
        private int InitTime { get; set; }
        private bool IsJumpPossible { get; set; }
        private Vector3 FleePosition { get; set; }

        public Flee()
        {
            Target = Vector3.Zero;
            FleePosition = Vector3.Zero;

            Dash.OnDash += OnDash;
            Drawing.OnDraw += OnDraw;
        }

        public override bool ShouldBeExecuted()
        {
            return Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Flee);
        }

        public override void Execute()
        {
            // A jump has been triggered, move into the set direction and
            // return the function to stop further calculations in the flee code
            if (Target != Vector3.Zero)
            {
                // Move to the target
                EloBuddy.Player.IssueOrder(GameObjectOrder.MoveTo, Target);

                // This is only to validate when the jump get aborted by, for example, stuns
                if (Environment.TickCount - InitTime > 500)
                {
                    Target = Vector3.Zero;
                    InitTime = 0;
                }
                else
                {
                    return;
                }
            }

            // Quick AAing without jumping over walls
            if (Settings.UseAutoAttacks && !Settings.UseWallJumps)
            {
                var dashObjects = VectorHelper.GetDashObjects();
                Orbwalker.ForcedTarget = dashObjects.Count > 0 ? dashObjects[0] : null;
            }

            // Wall jumping with possible AAing aswell
            if (Settings.UseWallJumps)
            {
                // We need to define a new move position since jumping over walls
                // requires you to be close to the specified wall. Therefore we set the move
                // point to be that specific piont. People will need to get used to it,
                // but this is how it works.
                var wallCheck = VectorHelper.GetFirstWallPoint(Player.Position, Game.CursorPos);

                // Be more precise
                if (wallCheck != null)
                {
                    wallCheck = VectorHelper.GetFirstWallPoint((Vector3)wallCheck, Game.CursorPos, 5);
                }

                // Define more position point
                var movePosition = wallCheck != null ? (Vector3)wallCheck : Game.CursorPos;

                // Update fleeTargetPosition
                var tempGrid = NavMesh.WorldToGrid(movePosition.X, movePosition.Y);
                FleePosition = NavMesh.GridToWorld((short)tempGrid.X, (short)tempGrid.Y);

                // Also check if we want to AA aswell
                Obj_AI_Base target = null;
                if (Settings.UseAutoAttacks)
                {
                    var dashObjects = VectorHelper.GetDashObjects();
                    if (dashObjects.Count > 0)
                    {
                        target = dashObjects[0];
                    }
                }

                // Reset walljump indicators
                IsJumpPossible = false;

                // Only calculate stuff when our Q is up and there is a wall inbetween
                if (Q.IsReady() && wallCheck != null)
                {
                    // Get our wall position to calculate from
                    var wallPosition = movePosition;

                    // Check 300 units to the cursor position in a 160 degree cone for a valid non-wall spot
                    var direction = (Game.CursorPos.To2D() - wallPosition.To2D()).Normalized();
                    const float maxAngle = 80f;
                    const float step = maxAngle / 20;
                    var currentAngle = 0f;
                    var currentStep = 0f;
                    var jumpTriggered = false;
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
                            currentAngle = (currentStep) * (float)Math.PI / 180;
                            currentStep += step;
                        }
                        else if (currentAngle > 0)
                        {
                            currentAngle = -currentAngle;
                        }

                        Vector3 checkPoint;

                        // One time only check for direct line of sight without rotating
                        if (currentStep == 0)
                        {
                            currentStep = step;
                            checkPoint = wallPosition + 300 * direction.To3D();
                        }
                        // Rotated check
                        else
                        {
                            checkPoint = wallPosition + 300 * direction.Rotated(currentAngle).To3D();
                        }

                        // Check if the point is not a wall
                        if (!checkPoint.ToNavMeshCell().CollFlags.HasFlag(CollisionFlags.Wall) &&
                            !checkPoint.ToNavMeshCell().CollFlags.HasFlag(CollisionFlags.Building))
                        {
                            // Check if there is a wall between the checkPoint and wallPosition
                            wallCheck = VectorHelper.GetFirstWallPoint(checkPoint, wallPosition);
                            if (wallCheck != null)
                            {
                                // There is a wall inbetween, get the closes point to the wall, as precise as possible
                                var wallPositionOpposite = (Vector3) VectorHelper.GetFirstWallPoint((Vector3)wallCheck, wallPosition, 5);

                                // Check if it's worth to jump considering the path length
                                if (Player.GetPath(wallPositionOpposite).Sum(o => o.To2D().LengthSquared()).Sqrt() - Player.Distance(wallPositionOpposite) > 200)
                                {
                                    // Check the distance to the opposite side of the wall
                                    if (Player.Distance(wallPositionOpposite, true) < Math.Pow(300 - Player.BoundingRadius / 2, 2))
                                    {
                                        // Make the jump happen
                                        InitTime = Environment.TickCount;
                                        Target = wallPositionOpposite;
                                        Q.Cast(wallPositionOpposite);

                                        // Update jumpTriggered value to not orbwalk now since we want to jump
                                        jumpTriggered = true;

                                        // Break the loop
                                        break;
                                    }
                                    // If we are not able to jump due to the distance, draw the spot to
                                    // make the user notice the possibliy
                                    // Update indicator values
                                    IsJumpPossible = true;
                                }
                            }
                        }
                    }

                    // Check if the loop triggered the jump, if not just orbwalk
                    if (!jumpTriggered)
                    {
                        Orbwalker.ForcedTarget = target;
                    }
                }
                // Either no wall or Q on cooldown, just move towards to wall then
                else
                {
                    Orbwalker.ForcedTarget = target;
                }
            }
        }

        private void OnDash(Obj_AI_Base sender, Dash.DashEventArgs args)
        {
            if (sender.IsMe)
            {
                InitTime = 0;
                Target = Vector3.Zero;
            }
        }

        private void OnDraw(EventArgs args)
        {
            // Flee position the player moves to
            if (FleePosition != Vector3.Zero)
            {
                Circle.Draw(IsJumpPossible ? Color.Green : SpellManager.Q.IsReady() ? Color.Red : Color.Teal, 50, 10, FleePosition);
            }
        }
    }
}
