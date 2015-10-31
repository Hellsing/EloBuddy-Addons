using System;
using EloBuddy;
using EloBuddy.SDK;
using SharpDX;

namespace Gnaaar
{
    public static class VectorHelper
    {
        public static Vector3? GetFirstWallPoint(this Vector3 from, Vector3 to, float step = 25)
        {
            var wallPoint = GetFirstWallPoint(from.To2D(), to.To2D(), step);
            if (wallPoint.HasValue)
            {
                return wallPoint.Value.To3DWorld();
            }
            return null;
        }

        public static Vector2? GetFirstWallPoint(this Vector2 from, Vector2 to, float step = 25)
        {
            var direction = (to - from).Normalized();

            for (float d = 0; d < from.Distance(to); d = d + step)
            {
                //var cell = (from + d * direction).WorldToGrid().ToNavMeshCell(); // TODO: Replace with this once CollFlags are fixed again and no -2 is needed
                var cell = ((from + d * direction).WorldToGrid() + 2).ToNavMeshCell();
                if (cell.CollFlags.HasFlag(CollisionFlags.Wall) ||
                    cell.CollFlags.HasFlag(CollisionFlags.Building))
                {
                    return from + d * direction;
                }
            }

            return null;
        }

        public static Vector3 Rotated(this Vector3 v, float angle)
        {
            var c = Math.Cos(angle);
            var s = Math.Sin(angle);
            return new Vector3((float) (v.X * c - v.Y * s), (float) (v.Y * c + v.X * s), v.Z);
        }
    }
}
