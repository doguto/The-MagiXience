using System;
using DG.Tweening;
using Project.Scenes.Battle.Scripts.Model;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Project.Scenes.Battle.Scripts.Runtime.Motion
{
    // 旧 AcceleratedMovementConfig。通常 / ワールド加速度 / 自機狙い放物線 の 3 モード
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

        [Header("Aimed parabola")]
        [SerializeField] bool aimAtPlayer;
        [SerializeField, Tooltip("ON でステージの外力の向きに曲げる")] bool useForceAxis = true;
        [SerializeField, Min(0.1f)] float targetFlightTime = 4f;
        [SerializeField, Min(0.001f)] float vertexDepth = 1.1f;
        [SerializeField, Min(0f)] float vertexScreenMargin = 0.35f;
        [SerializeField, Min(0)] int rightShotInterval;
        [SerializeField] bool alternateVertical;
        [SerializeField, Range(0f, 0.4f)] float flightTimeVariation;
        [SerializeField, Range(0f, 0.8f)] float vertexDepthVariation;

        public override string Summary => aimAtPlayer ? $"自機狙い放物線 {targetFlightTime:0.##}s"
            : useWorldAcceleration ? $"放物運動 初速 {initialSpeed:0.##}" : $"加速 {initialSpeed:0.##} → {acceleration:+0.##;-0.##}/s";

        public override MotionRunner CreateRunner()
        {
            if (aimAtPlayer) return CreateAimed();

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

        MotionRunner CreateAimed()
        {
            Vector2 start = default, velocity = default, accel = default;
            var z = 0f;
            var aimed = false;
            return new DelegatePullRunner(duration, false, (r, _) =>
            {
                if (!aimed) return;
                var p = Kinematics.Position(start, velocity, accel, r.Time);
                r.Target.Position = new Vector3(p.x, p.y, z);
            }, r =>
            {
                if (!BattleTargets.TryGetPlayerPosition(out var player)) return;
                aimed = true;
                start = r.Target.Position;
                z = r.Target.Position.z;
                Vector2 axis = useForceAxis ? r.LaunchForceAxis : worldAcceleration.normalized;
                var shot = AimedParabola.NextShotIndex();
                if (alternateVertical) axis = shot % 2 == 0 ? Vector2.up : Vector2.down;
                var flightTime = Mathf.Max(0.1f, targetFlightTime * (1f + flightTimeVariation * ((shot % 11) / 5f - 1f)));
                var depth = Mathf.Max(0.001f, vertexDepth * (1f + vertexDepthVariation * ((shot % 7) / 3f - 1f)));
                var screen = Rect.MinMaxRect(ScreenBoundsCache.MinX, ScreenBoundsCache.MinY, ScreenBoundsCache.MaxX, ScreenBoundsCache.MaxY);
                AimedParabola.SolveMixed(start, player, axis, screen, flightTime, depth, vertexScreenMargin,
                    rightShotInterval > 0 && shot % rightShotInterval == 0, out velocity, out accel);
            });
        }
    }
}
