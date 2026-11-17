using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Runtime.Motion
{
    // 自機狙いの放物線。画面内に頂点を作り、指定秒数で発射時の自機位置へ到達する。
    public static class AimedParabola
    {
        static int shotIndex;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetShotIndex() => shotIndex = 0;

        // 全弾で共有する発射カウンタ。到達時間・深さの揺らぎと右撃ちの間隔に使う
        public static int NextShotIndex() => shotIndex = (shotIndex + 1) % 1000000;

        public static void SolveMixed(Vector2 start, Vector2 target, Vector2 preferredDirection,
            Rect screen, float flightTime, float bendDepth, float edgeMargin, bool rightShot,
            out Vector2 velocity, out Vector2 acceleration)
        {
            if (rightShot && TrySolve(start, target, Vector2.right, screen, flightTime, bendDepth, edgeMargin,
                    out velocity, out acceleration)) return;
            SolveVertical(start, target, preferredDirection, screen, flightTime, bendDepth, edgeMargin,
                out velocity, out acceleration);
        }

        static void SolveVertical(Vector2 start, Vector2 target, Vector2 preferredDirection,
            Rect screen, float flightTime, float bendDepth, float edgeMargin,
            out Vector2 velocity, out Vector2 acceleration)
        {
            flightTime = Mathf.Max(0.1f, flightTime);
            float lowerRoom = Mathf.Min(start.y, target.y) - screen.yMin;
            float upperRoom = screen.yMax - Mathf.Max(start.y, target.y);
            Vector2 roomyAxis = lowerRoom >= upperRoom ? Vector2.up : Vector2.down;
            Vector2 axis = Mathf.Abs(preferredDirection.y) > 0.001f
                ? (preferredDirection.y > 0f ? Vector2.up : Vector2.down) : roomyAxis;

            if (TrySolve(start, target, axis, screen, flightTime, bendDepth, edgeMargin, out velocity, out acceleration)
                || TrySolve(start, target, -axis, screen, flightTime, bendDepth, edgeMargin, out velocity, out acceleration)) return;

            // 画面端どうしなど画面内に頂点を作れない場合も、狙いを保った縦の放物線にする
            axis = roomyAxis;
            float room = Mathf.Max(lowerRoom, upperRoom);
            float depth = Mathf.Max(0.001f, bendDepth);
            if (room > 0.0001f)
                depth = Mathf.Min(depth, room - Mathf.Min(Mathf.Max(0.001f, edgeMargin), room * 0.5f));
            float q0 = start.y * axis.y, q1 = target.y * axis.y;
            float qVertex = Mathf.Min(q0, q1) - depth;
            float root0 = Mathf.Sqrt(q0 - qVertex), root1 = Mathf.Sqrt(q1 - qVertex);
            float sum = root0 + root1;
            acceleration = axis * (2f * sum * sum / (flightTime * flightTime));
            velocity = new Vector2((target.x - start.x) / flightTime, -axis.y * 2f * root0 * sum / flightTime);
        }

        static bool TrySolve(Vector2 start, Vector2 target, Vector2 axis,
            Rect screen, float flightTime, float bendDepth, float edgeMargin,
            out Vector2 velocity, out Vector2 acceleration)
        {
            velocity = acceleration = Vector2.zero;
            if (flightTime < 0.1f || axis.sqrMagnitude < 0.5f) return false;
            axis.Normalize();
            if (Mathf.Abs(axis.x * axis.y) > 0.001f) return false;

            var perpendicular = new Vector2(-axis.y, axis.x);
            float q0 = Vector2.Dot(start, axis), q1 = Vector2.Dot(target, axis);
            float lowerBound = axis.x > 0.5f ? screen.xMin : axis.x < -0.5f ? -screen.xMax
                : axis.y > 0.5f ? screen.yMin : -screen.yMax;
            float room = Mathf.Min(q0, q1) - lowerBound;
            if (room <= 0.0001f || !screen.Contains(start) || !screen.Contains(target)) return false;

            float margin = Mathf.Min(Mathf.Max(0.001f, edgeMargin), room * 0.5f);
            float depth = Mathf.Min(Mathf.Max(0.001f, bendDepth), room - margin);
            float qVertex = Mathf.Min(q0, q1) - depth;
            float root0 = Mathf.Sqrt(q0 - qVertex), root1 = Mathf.Sqrt(q1 - qVertex);
            float sum = root0 + root1;
            float a = 2f * sum * sum / (flightTime * flightTime);
            velocity = axis * (-2f * root0 * sum / flightTime)
                + perpendicular * (Vector2.Dot(target - start, perpendicular) / flightTime);
            acceleration = axis * a;
            return true;
        }
    }
}
