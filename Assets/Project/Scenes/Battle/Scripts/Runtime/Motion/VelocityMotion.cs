using System;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Runtime.Motion
{
    // 旧 InfiniteMovementConfig。発射方向があればその向きに |velocity| の速さで進む
    [Serializable]
    public sealed class VelocityMotion : MotionStep
    {
        [SerializeField] Vector3 velocity = Vector3.left;
        [SerializeField, Min(0f), Tooltip("0 で無限")] float duration;

        public VelocityMotion() { }
        public VelocityMotion(Vector3 velocity) => this.velocity = velocity;

        public override string Summary => $"速度 ({velocity.x:0.##}, {velocity.y:0.##})" + (duration > 0f ? $" / {duration:0.##}s" : "");

        public override MotionRunner CreateRunner()
        {
            var v = velocity;
            return new DelegatePullRunner(duration, true, (r, dt) => r.Target.Position += v * dt, r =>
            {
                if (r.LaunchDirection != Vector2.zero) v = (Vector3)r.LaunchDirection.normalized * velocity.magnitude;
            });
        }
    }
}
