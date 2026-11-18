using System;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Runtime.Motion
{
    // 旧 TweenMovementConfig
    [Serializable]
    public sealed class MoveToMotion : MotionStep
    {
        [SerializeField] Vector3 targetOffset = new(-3f, 0f, 0f);
        [SerializeField, Tooltip("ON: 現在位置からの相対 / OFF: ワールド座標")] bool isRelative = true;
        [SerializeField, Min(0f)] float duration = 1f;
        [SerializeField] MotionEase ease = MotionEase.Of(DG.Tweening.Ease.Linear);

        public MoveToMotion() { }

        public MoveToMotion(Vector3 targetOffset, bool isRelative, float duration, MotionEase ease)
        {
            this.targetOffset = targetOffset;
            this.isRelative = isRelative;
            this.duration = duration;
            this.ease = ease;
        }

        public float Duration => duration;
        public Vector3 ResolveDestination(Vector3 from) => isRelative ? from + targetOffset : targetOffset;
        public override string Summary => (isRelative ? $"相対 {targetOffset}" : $"位置 {targetOffset}") + $" / {duration:0.##}s {ease}";

        public override MotionRunner CreateRunner() => new TweenRunner(duration, ease, b => ResolveDestination(b.Position));
    }
}
