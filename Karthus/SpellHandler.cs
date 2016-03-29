using System;
using EloBuddy;
using EloBuddy.SDK;
using SharpDX;

namespace Karthus
{
    public sealed class SpellHandler
    {
        private Karthus Instance { get; set; }

        public Spell.Skillshot Q { get; private set; }
        public Spell.Skillshot W { get; private set; }
        public Spell.Active E { get; private set; }
        public Spell.Active R { get; private set; }

        public float WallOfPainMaxRangeSqr
        {
            get { return W.RangeSquared + (WallOfPainWidth / 2).Pow(); }
        }

        public float WallOfPainMaxRange
        {
            get { return (float) Math.Sqrt(WallOfPainMaxRangeSqr); }
        }

        public float WallOfPainWidth
        {
            get { return 700 + 100 * W.Level; }
        }

        public SpellHandler(Karthus instance, Spell.Skillshot q = null, Spell.Skillshot w = null, Spell.Active e = null, Spell.Active r = null)
        {
            // Initialize properties
            Instance = instance;
            Q = q;
            W = w;
            E = e;
            R = r;
        }

        public bool CastOnBestTarget(SpellSlot slot)
        {
            // Get the target
            var target = TargetSelector.GetTarget(GetRangeFromSlot(slot), DamageType.Magical);

            // Validate target
            if (target == null)
            {
                return slot == SpellSlot.E && IsDefileActive() && E.Cast();
            }

            switch (slot)
            {
                case SpellSlot.Q:
                {
                    if (Q.Cast(target))
                    {
                        // Q was casted
                        return true;
                    }
                    break;
                }
                case SpellSlot.W:
                {
                    if (CastWallOfPain(target))
                    {
                        // W was casted
                        return true;
                    }
                    break;
                }
                case SpellSlot.E:
                {
                    if (!IsDefileActive() && E.IsInRange(target) && E.Cast())
                    {
                        // E was casted
                        return true;
                    }
                    break;
                }
                case SpellSlot.R:
                {
                    if (Player.Instance.GetSpellDamage(target, R.Slot) > target.TotalShieldHealth() + 100 && R.Cast())
                    {
                        // R was casted
                        return true;
                    }
                    break;
                }
            }

            return false;
        }

        public float GetRangeFromSlot(SpellSlot slot)
        {
            switch (slot)
            {
                case SpellSlot.Q:
                    return Q.Range;
                case SpellSlot.W:
                    return W.Range;
                case SpellSlot.E:
                    return E.Range;
                case SpellSlot.R:
                    return R.Range;
            }

            return float.MaxValue;
        }

        public bool IsDefileActive()
        {
            return E.Handle.ToggleState == 2;
        }

        public bool IsInWallOfPainRange(Vector2 position)
        {
            return Player.Instance.Distance(position, true) < WallOfPainMaxRangeSqr;
        }

        public bool IsInWallOfPainRange(Vector3 position)
        {
            return IsInWallOfPainRange(position.To2D());
        }

        public bool IsInWallOfPainRange(Obj_AI_Base target)
        {
            return IsInWallOfPainRange(target.ServerPosition.To2D());
        }

        public bool CastWallOfPain(AIHeroClient target = null)
        {
            // Get the target
            target = target ?? TargetSelector.GetTarget(WallOfPainMaxRange, DamageType.Magical);

            // Validate target
            if (target == null)
            {
                return false;
            }

            // Get predicted position after the delay
            var targetPosition = Prediction.Position.PredictUnitPosition(target, W.CastDelay).To3DWorld();

            // Check if target is in range
            if (IsInWallOfPainRange(targetPosition))
            {
                // Check if target is in reglar W range
                if (W.IsInRange(targetPosition) && W.Cast(targetPosition))
                {
                    // W was casted
                    return true;
                }

                // Extended range
                var x = W.Range;
                var y = (float) Math.Sqrt(Player.Instance.Distance(targetPosition, true) - W.RangeSquared);
                var z = Player.Instance.Distance(targetPosition);
                var angle = (float) Math.Acos((y.Pow() + z.Pow() - x.Pow()) / (2 * y * z));
                var direction = (Player.Instance.ServerPosition.To2D() - targetPosition.To2D()).Normalized().Rotated(angle);
                var castPosition = (targetPosition.To2D() + y * direction).To3DWorld();

                // Final check if the cast position is in range (should always be true)
                if (W.IsInRange(castPosition) && W.Cast(castPosition))
                {
                    // W was casted
                    return true;
                }
            }

            return false;
        }
    }
}
