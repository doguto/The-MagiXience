using System;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Runtime.Motion
{
    // 旧 QuadraticMovementConfig
    [Serializable]
    public sealed class QuadraticMotion : MotionStep
    {
        [SerializeField] Vector3 initialVelocity = Vector3.left;
        [SerializeField] Vector3 acceleration;
        [SerializeField, Min(0f), Tooltip("0 で無限")] float duration;

        public override string Summary => $"初速 {initialVelocity} / 加速 {acceleration}";

        public override MotionRunner CreateRunner()
        {
            var v = initialVelocity;
            return new DelegatePullRunner(duration, false, (r, dt) =>
            {
                v += acceleration * dt;
                r.Target.Position += v * dt;
            });
        }
    }
}
