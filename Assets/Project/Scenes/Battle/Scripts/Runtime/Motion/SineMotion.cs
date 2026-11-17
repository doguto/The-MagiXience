using System;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Runtime.Motion
{
    // 旧 SineMovementConfig。差分で加算するため外部の移動と合成できる
    [Serializable]
    public sealed class SineMotion : MotionStep
    {
        [SerializeField] Vector3 baseVelocity = Vector3.left;
        [SerializeField, Min(0f)] float amplitude = 1f;
        [SerializeField, Min(0.01f), Tooltip("Hz")] float frequency = 1f;
        [SerializeField, Min(0f), Tooltip("0 で無限")] float duration;

        public override string Summary => $"サイン波 振幅 {amplitude:0.##} / {frequency:0.##}Hz";

        public override MotionRunner CreateRunner()
        {
            var axis = new Vector3(-baseVelocity.y, baseVelocity.x, 0f).normalized;
            var prev = 0f;
            return new DelegatePullRunner(duration, true, (r, dt) =>
            {
                var s = Mathf.Sin(r.Time * frequency * 2f * Mathf.PI) * amplitude;
                r.Target.Position += baseVelocity * dt + axis * (s - prev);
                prev = s;
            });
        }
    }
}
