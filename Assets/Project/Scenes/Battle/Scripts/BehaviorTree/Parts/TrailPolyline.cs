using UnityEngine;

namespace Project.Scenes.Battle.Scripts.BehaviorTree
{
    // 開始位置を確定させた経路。距離で位置と区間を引く
    public sealed class TrailPolyline
    {
        readonly Vector2[] points;
        readonly float[] cumulative;

        public TrailPolyline(Vector2 start, TrailPath path)
        {
            var segments = path.Segments;
            points = new Vector2[segments.Count + 1];
            cumulative = new float[segments.Count + 1];
            points[0] = start;
            for (var i = 0; i < segments.Count; i++)
            {
                points[i + 1] = path.Relative ? points[i] + segments[i].position : segments[i].position;
                cumulative[i + 1] = cumulative[i] + Vector2.Distance(points[i], points[i + 1]);
            }
            Length = cumulative[segments.Count];
        }

        public float Length { get; }

        public int SegmentAt(float distance)
        {
            for (var i = 1; i < cumulative.Length; i++)
            {
                if (distance <= cumulative[i]) return i - 1;
            }
            return Mathf.Max(0, cumulative.Length - 2);
        }

        public Vector2 PositionAt(float distance)
        {
            if (points.Length == 1) return points[0];
            var i = SegmentAt(distance);
            var length = cumulative[i + 1] - cumulative[i];
            var t = length > 1e-6f ? (distance - cumulative[i]) / length : 1f;
            return Vector2.Lerp(points[i], points[i + 1], Mathf.Clamp01(t));
        }

        public Vector2 PositionAtProgress(int elapsedFrames, int totalFrames) =>
            PositionAt(Length * Mathf.Clamp01(elapsedFrames / (float)totalFrames));
    }
}
