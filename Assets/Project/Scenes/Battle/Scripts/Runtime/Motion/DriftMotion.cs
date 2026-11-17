using System;
using DG.Tweening;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Project.Scenes.Battle.Scripts.Runtime.Motion
{
    // 旧 DriftMovementConfig。anchor の周りを「移動 → 停止」で漂う
    [Serializable]
    public sealed class DriftMotion : MotionStep
    {
        [SerializeField] Vector3 anchor = new(-2f, 0f, 0f);
        [SerializeField] float pullStrength = 0.5f;
        [SerializeField, Min(0.01f)] float moveDistance = 0.5f;
        [SerializeField, Tooltip("移動範囲 (幅, 高さ)")] Vector2 moveBounds = new(3f, 2f);
        [SerializeField, Min(0.01f)] float moveDuration = 0.6f;
        [SerializeField, Min(0f)] float pauseDuration = 0.3f;
        [SerializeField] Ease moveEase = Ease.InOutSine;
        [SerializeField, Min(0f), Tooltip("0 で無限")] float duration;

        public override string Summary => $"{anchor} 周りを漂う";

        public override MotionRunner CreateRunner()
        {
            var cycleDuration = moveDuration + pauseDuration;
            Vector3 moveStart = default, moveEnd = default;
            var cycle = 0f;
            return new DelegatePullRunner(duration, false, (r, dt) =>
            {
                cycle += dt;
                if (cycle >= cycleDuration)
                {
                    r.Target.Position = moveEnd;
                    moveStart = moveEnd;
                    moveEnd = NextTarget(moveStart);
                    cycle -= cycleDuration;
                }
                r.Target.Position = cycle < moveDuration
                    ? Vector3.Lerp(moveStart, moveEnd, MotionEasing.Evaluate(moveEase, cycle / moveDuration))
                    : moveEnd;
            }, r =>
            {
                moveStart = r.Target.Position;
                moveEnd = NextTarget(moveStart);
            });
        }

        Vector3 NextTarget(Vector3 current)
        {
            var toAnchor = anchor - current;
            var pull = toAnchor.normalized * Mathf.Min(pullStrength, toAnchor.magnitude);
            var angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            var combined = pull + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);
            var candidate = current + (combined.sqrMagnitude > 0f ? combined.normalized : Vector3.right) * moveDistance;
            candidate.x = Mathf.Clamp(candidate.x, anchor.x - moveBounds.x * 0.5f, anchor.x + moveBounds.x * 0.5f);
            candidate.y = Mathf.Clamp(candidate.y, anchor.y - moveBounds.y * 0.5f, anchor.y + moveBounds.y * 0.5f);
            return candidate;
        }
    }
}
