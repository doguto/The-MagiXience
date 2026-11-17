using System;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Runtime.Motion
{
    // 旧 ForwardMovementConfig。開始時の向き (right) に進む
    [Serializable]
    public sealed class ForwardMotion : MotionStep
    {
        [SerializeField] float speed = 5f;
        [SerializeField, Min(0f), Tooltip("0 で無限")] float duration;

        public override string Summary => $"前方へ {speed:0.##}";

        public override MotionRunner CreateRunner()
        {
            var v = Vector3.zero;
            return new DelegatePullRunner(duration, true, (r, dt) => r.Target.Position += v * dt,
                r => v = r.Target.Rotation * Vector3.right * speed);
        }
    }
}
