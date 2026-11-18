using System;
using DG.Tweening;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Project.Scenes.Battle.Scripts.Runtime.Motion
{
    // 旧 ZigzagMovementConfig
    [Serializable]
    public sealed class ZigzagMotion : MotionStep
    {
        [SerializeField, Min(0f)] float speed = 6f;
        [SerializeField, Tooltip("基準方向からの振れ角 (度)")] float angleDegrees = 45f;
        [SerializeField, Min(0.01f), Tooltip("折り返すまでの距離")] float segmentLength = 1f;
        [SerializeField, Tooltip("最初に振れる向き (0以上で +)")] int initialSign = 1;
        [SerializeField, Tooltip("0 で無制限")] int maxReflections;
        [SerializeField, Tooltip("0 で Segment Length と同じ")] float firstSegmentLength;
        [SerializeField, Min(0f), Tooltip("0 で無限")] float duration;
        [SerializeField] Vector2 fallbackAxis = Vector2.left;
        [SerializeField] bool rotateToVelocity;
        [SerializeField] float rotationOffsetDegrees;

        public override string Summary => $"ジグザグ {speed:0.##} / ±{angleDegrees:0.#}°";

        public override MotionRunner CreateRunner()
        {
            float axisAngle = 0f, traveled = 0f, currentSegment = 0f;
            int sign = 1, reflections = 0;
            var velocity = Vector2.zero;

            void ApplyRotation(IMotionBody body)
            {
                if (!rotateToVelocity) return;
                body.Rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg + rotationOffsetDegrees);
            }

            return new DelegatePullRunner(duration, false, (r, dt) =>
            {
                var remaining = speed * dt;
                while (remaining > 0f)
                {
                    var canTurn = maxReflections <= 0 || reflections < maxReflections;
                    var toTurn = canTurn ? currentSegment - traveled : remaining;
                    var move = Mathf.Min(remaining, toTurn);
                    r.Target.Position += (Vector3)(velocity * move);
                    remaining -= move;
                    if (!canTurn) break;
                    traveled += move;
                    if (traveled < currentSegment - Mathf.Epsilon) break;
                    traveled = 0f;
                    currentSegment = segmentLength;
                    sign = -sign;
                    reflections++;
                    velocity = AngleToDirection(axisAngle + angleDegrees * sign);
                    ApplyRotation(r.Target);
                }
            }, r =>
            {
                var axis = r.LaunchDirection != Vector2.zero ? r.LaunchDirection.normalized : fallbackAxis.normalized;
                if (axis == Vector2.zero) axis = Vector2.left;
                axisAngle = Mathf.Atan2(axis.y, axis.x) * Mathf.Rad2Deg;
                sign = initialSign >= 0 ? 1 : -1;
                currentSegment = firstSegmentLength > 0f ? firstSegmentLength : segmentLength;
                velocity = AngleToDirection(axisAngle + angleDegrees * sign);
                ApplyRotation(r.Target);
            });
        }

        static Vector2 AngleToDirection(float degrees)
        {
            var rad = degrees * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
        }
    }
}
