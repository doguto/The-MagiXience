using System;
using DG.Tweening;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Project.Scenes.Battle.Scripts.Runtime.Motion
{
    // 旧 VertexParabolaMovementConfig
    [Serializable]
    public sealed class VertexParabolaMotion : MotionStep
    {
        [SerializeField] ParabolaAxis orientation = ParabolaAxis.Vertical;
        [SerializeField, Tooltip("頂点。Is Relative なら発射位置からの相対")] Vector2 vertex = new(-3f, 4.5f);
        [SerializeField, Min(0.01f)] float initialSpeed = 5f;
        [SerializeField] bool isRelative = true;
        [SerializeField, Tooltip("発射時刻で頂点を揺らす")] bool oscillateVertex;
        [SerializeField] Vector2 vertexAmplitude;
        [SerializeField, Tooltip("rad/s")] Vector2 vertexAngularSpeed;
        [SerializeField] float attackTimeOffset;

        public VertexParabolaMotion() { }

        public VertexParabolaMotion(Vector2 vertex, float initialSpeed, bool isRelative, ParabolaAxis orientation = ParabolaAxis.Vertical)
        {
            this.vertex = vertex;
            this.initialSpeed = initialSpeed;
            this.isRelative = isRelative;
            this.orientation = orientation;
        }

        public override string Summary => $"放物線 頂点 {vertex}{(isRelative ? " (相対)" : "")} / 速さ {initialSpeed:0.##}";

        public override MotionRunner CreateRunner()
        {
            Vector2 start = default, velocity = default, accel = default;
            var z = 0f;
            DelegatePullRunner runner = null;
            runner = new DelegatePullRunner(0f, false, (r, _) =>
            {
                var p = Kinematics.Position(start, velocity, accel, r.Time);
                r.Target.Position = new Vector3(p.x, p.y, z);
            });
            return new BeginHook(runner, r =>
            {
                start = r.Target.Position;
                z = r.Target.Position.z;
                var t = Mathf.Max(0f, r.LaunchAttackTime - attackTimeOffset);
                var offset = oscillateVertex
                    ? new Vector2(vertexAmplitude.x * Mathf.Sin(t * vertexAngularSpeed.x), vertexAmplitude.y * Mathf.Sin(t * vertexAngularSpeed.y))
                    : Vector2.zero;
                var v = vertex + offset;
                var worldVertex = isRelative ? start + v : v;
                if (!VertexParabola.TrySolve(start, worldVertex, orientation, initialSpeed, out velocity, out accel))
                {
                    Debug.LogWarning($"[VertexParabolaMotion] Invalid parabola. start={start} vertex={worldVertex} speed={initialSpeed}");
                    r.Reject();
                }
            });
        }
    }
}
