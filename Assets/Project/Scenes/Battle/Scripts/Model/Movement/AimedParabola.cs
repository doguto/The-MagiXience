using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Model.Movement
{
    public static class AimedParabola
    {
#if UNITY_EDITOR
        public static int CurvedShots { get; private set; }
        public static int CalmShots { get; private set; }
        public static int FallbackShots { get; private set; }
        public static float MaxAimError { get; private set; }
        public static int WindDirectionsUsed { get; private set; }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetDiagnostics()
        { CurvedShots = CalmShots = FallbackShots = WindDirectionsUsed = 0; MaxAimError = 0; }
        public static void RecordLaunch(Vector2 start, Vector2 aim, Vector2 velocity, Vector2 acceleration,
            float flightTime, bool calm, bool solved)
        {
            if (solved) CurvedShots++;
            else if (calm) CalmShots++;
            else FallbackShots++;
            if (solved) WindDirectionsUsed |= acceleration.x < 0 ? 4 : acceleration.y > 0 ? 1 : 2;
            MaxAimError = Mathf.Max(MaxAimError, Vector2.Distance(Position(start, velocity, acceleration, flightTime), aim));
        }
#endif
        // Positive acceleration along axis. The vertex is reached before the target.
        // q(t) = qVertex + a/2 * (t-tVertex)^2; the perpendicular coordinate is linear.
        public static bool TrySolve(Vector2 start, Vector2 target, Vector2 axis,
            Rect screen, float flightTime, float bendDepth, float edgeMargin,
            out Vector2 velocity, out Vector2 acceleration, out Vector2 vertex)
        {
            velocity = acceleration = vertex = Vector2.zero;
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
            float vertexTime = flightTime * root0 / sum;
            vertex = Position(start, velocity, acceleration, vertexTime);
            return true;
        }

        public static Vector2 Position(Vector2 start, Vector2 velocity, Vector2 acceleration, float time)
            => start + velocity * time + acceleration * (0.5f * time * time);
    }
}
