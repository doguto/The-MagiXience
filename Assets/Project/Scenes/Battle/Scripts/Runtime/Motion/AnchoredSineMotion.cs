using System;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Runtime.Motion
{
    // 旧 AnchoredSineMovementConfig。絶対位置で動くため開始時に anchor へ吸着する
    [Serializable]
    public sealed class AnchoredSineMotion : MotionStep
    {
        [SerializeField] Vector3 anchor = new(5.7f, 0f, 0f);
        [SerializeField] Vector3 amplitude = new(0f, 1.4f, 0f);
        [SerializeField, Tooltip("rad/s")] float angularSpeed = 0.65f;
        [SerializeField, Min(0f)] float cueSeconds = 0.9f;
        [SerializeField] Vector3 cueOffset;
        [SerializeField, Min(0f), Tooltip("0 で無限")] float duration;

        public override string Summary => $"{anchor} 周りで揺れる";

        public override MotionRunner CreateRunner() => new DelegatePullRunner(duration, false, (r, _) =>
        {
            var e = r.Time;
            r.Target.Position = e < cueSeconds && cueSeconds > 0f
                ? anchor + cueOffset * Mathf.Sin(Mathf.PI * e / cueSeconds)
                : anchor + amplitude * Mathf.Sin((e - cueSeconds) * angularSpeed);
        });
    }
}
