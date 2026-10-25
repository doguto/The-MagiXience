using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Serialization;

namespace Project.Scenes.Battle.Scripts.Model.Movement
{
    public enum ParabolaOrientation
    {
        [InspectorName("縦")] Vertical = 0,
        // Preserve the serialized value of the former Right option.
        [InspectorName("横")] Horizontal = 2
    }

    [Serializable]
    public class VertexParabolaMovementConfig : IMovementStep, ISerializationCallbackReceiver
    {
        [SerializeField, FormerlySerializedAs("bulge"), Tooltip("縦：上下に折り返す。横：左右に折り返す。凸の向きは発射位置と頂点から自動で決まります。")]
        ParabolaOrientation orientation = ParabolaOrientation.Vertical;
        [SerializeField, Tooltip("移動開始位置からの頂点の相対座標（ワールド軸）。Xは右、Yは上が正です。")]
        Vector2 vertex = new(-3f, 4.5f);
        [SerializeField, Min(0.01f), Tooltip("発射時の速さ（ワールド単位/秒）。軌道を保ったまま速さを変更できます。飛行中は等速ではありません。")]
        float initialSpeed = 5f;

        public Tween Play(Transform target, Vector2 overrideDirection, Animator animator)
        {
            Vector2 start = target.position;
            Vector2 worldVertex = start + vertex;
            if (!VertexParabola.TrySolve(start, worldVertex, orientation, initialSpeed, out var velocity, out var acceleration, out _))
            {
                Debug.LogWarning($"[VertexParabola] {target.name}: invalid {orientation} parabola. Start={start}, VertexOffset={vertex}, Vertex={worldVertex}, InitialSpeed={initialSpeed}. " +
                    "The vertex must differ from the start in both X and Y, with a positive finite speed.", target);
                return null; // BulletEntityPresenter returns rejected shots to their pool.
            }
            float elapsed = 0f;
            float z = target.position.z;
            return PullMovementHelper.Create(target, 0f, (t, dt) =>
            {
                elapsed += dt;
                Vector2 p = VertexParabola.Position(start, velocity, acceleration, elapsed);
                t.position = new Vector3(p.x, p.y, z);
            }, Ease.Linear);
        }

        public void OnBeforeSerialize() { }

        public void OnAfterDeserialize()
        {
            // Former Up=0 and Down=1 both become Vertical; Right=2 remains Horizontal.
            if ((int)orientation == 1) orientation = ParabolaOrientation.Vertical;
        }
    }

    public static class VertexParabola
    {
        // Axis-aligned, constant-acceleration parabola, travelling towards the vertex first.
        public static bool TrySolve(Vector2 start, Vector2 vertex, ParabolaOrientation orientation, float speed,
            out Vector2 velocity, out Vector2 acceleration, out float vertexTime)
        {
            velocity = acceleration = Vector2.zero;
            vertexTime = 0f;
            if (!Finite(start.x) || !Finite(start.y) || !Finite(vertex.x) || !Finite(vertex.y) || !Finite(speed) || speed <= 0f)
                return false;
            Vector2 axis;
            switch (orientation)
            {
                case ParabolaOrientation.Vertical:
                    axis = start.y > vertex.y ? Vector2.up : Vector2.down;
                    break;
                case ParabolaOrientation.Horizontal:
                    axis = start.x > vertex.x ? Vector2.right : Vector2.left;
                    break;
                default: return false;
            }
            Vector2 tangent = new(-axis.y, axis.x);
            Vector2 offset = start - vertex;
            float q = Vector2.Dot(offset, axis), r = Vector2.Dot(offset, tangent);
            if (q <= 0.00001f || Mathf.Abs(r) <= 0.00001f) return false;

            // q(t) = q (1 - t/T)^2; r(t) = r (1 - t/T).
            // |v(0)| = sqrt(4q² + r²) / T, so the speed uniquely determines T.
            vertexTime = Mathf.Sqrt(4f * q * q + r * r) / speed;
            if (!Finite(vertexTime) || vertexTime <= 0f) return false;
            velocity = (-2f * q * axis - r * tangent) / vertexTime;
            acceleration = axis * (2f * q / (vertexTime * vertexTime));
            return Finite(velocity.x) && Finite(velocity.y) && Finite(acceleration.x) && Finite(acceleration.y)
                && acceleration.sqrMagnitude > 0f;
        }

        public static Vector2 Position(Vector2 start, Vector2 velocity, Vector2 acceleration, float time)
            => start + velocity * time + 0.5f * acceleration * time * time;

        static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
