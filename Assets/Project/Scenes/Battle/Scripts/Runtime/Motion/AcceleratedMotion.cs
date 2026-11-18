using System;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Runtime.Motion
{
    // 旧 AcceleratedMovementConfig。通常 / ワールド加速度 の 2 モード
    [Serializable]
    public sealed class AcceleratedMotion : MotionStep
    {
        [SerializeField] Vector2 direction = Vector2.left;
        [SerializeField, Tooltip("ON で発射方向より Direction を優先する")] bool useConfiguredDirection;
        [SerializeField, Min(0f)] float initialSpeed = 2f;
        [SerializeField, Tooltip("負の値で減速")] float acceleration = 1f;
        [SerializeField, Min(0f)] float maxSpeed = 10f;
        [SerializeField, Min(0f)] float minSpeed;
        [SerializeField, Min(0f), Tooltip("0 で無限")] float duration;
        [SerializeField, Tooltip("固定方向の加速度で放物運動する")] bool useWorldAcceleration;
        [SerializeField] Vector2 worldAcceleration = Vector2.down;

        public override string Summary =>
            useWorldAcceleration ? $"放物運動 初速 {initialSpeed:0.##}" : $"加速 {initialSpeed:0.##} → {acceleration:+0.##;-0.##}/s";

        public override MotionRunner CreateRunner()
        {
            var v = Vector3.zero;
            var dir = Vector3.zero;
            return new DelegatePullRunner(duration, false, (r, dt) =>
            {
                if (useWorldAcceleration)
                {
                    Vector3 g = worldAcceleration;
                    r.Target.Position += v * dt + 0.5f * g * dt * dt;
                    v += g * dt;
                    return;
                }
                v += dir * acceleration * dt;
                var speed = v.magnitude;
                if (maxSpeed > 0f && speed > maxSpeed) v = v.normalized * maxSpeed;
                if (speed < minSpeed) v = speed > 0f ? v.normalized * minSpeed : dir * minSpeed;
                r.Target.Position += v * dt;
            }, r =>
            {
                dir = ((Vector3)(!useConfiguredDirection && r.LaunchDirection != Vector2.zero ? r.LaunchDirection : direction)).normalized;
                v = dir * initialSpeed;
            });
        }
    }
}
