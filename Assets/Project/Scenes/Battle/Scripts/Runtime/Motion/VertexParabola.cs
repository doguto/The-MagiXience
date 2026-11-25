using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Runtime.Motion
{
    // 頂点指定の放物線。発射位置から頂点へ向かい、頂点で折り返す軸平行の等加速度運動。
    public static class VertexParabola
    {
        public static bool TrySolve(Vector2 start, Vector2 vertex, ParabolaAxis orientation, float speed,
            out Vector2 velocity, out Vector2 acceleration)
        {
            velocity = acceleration = Vector2.zero;
            if (!Finite(start.x) || !Finite(start.y) || !Finite(vertex.x) || !Finite(vertex.y) || !Finite(speed) || speed <= 0f)
                return false;

            Vector2 axis = orientation == ParabolaAxis.Horizontal
                ? (start.x > vertex.x ? Vector2.right : Vector2.left)
                : (start.y > vertex.y ? Vector2.up : Vector2.down);

            var tangent = new Vector2(-axis.y, axis.x);
            var offset = start - vertex;
            float q = Vector2.Dot(offset, axis), r = Vector2.Dot(offset, tangent);
            if (q <= 0.00001f || Mathf.Abs(r) <= 0.00001f) return false;

            var vertexTime = Mathf.Sqrt(4f * q * q + r * r) / speed;
            if (!Finite(vertexTime) || vertexTime <= 0f) return false;

            velocity = (-2f * q * axis - r * tangent) / vertexTime;
            acceleration = axis * (2f * q / (vertexTime * vertexTime));
            return Finite(velocity.x) && Finite(velocity.y) && Finite(acceleration.x) && Finite(acceleration.y)
                && acceleration.sqrMagnitude > 0f;
        }

        static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
