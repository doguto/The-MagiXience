using System;
using DG.Tweening;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Project.Scenes.Battle.Scripts.Runtime.Motion
{
    // 旧 SpiralMovementConfig (ChargeSignal が生成していたもの)。中心の周りを回りながら半径を変える
    [Serializable]
    public sealed class SpiralMotion : MotionStep
    {
        [SerializeField, Min(0f)] float startRadius = 3.5f;
        [SerializeField, Min(0f)] float targetRadius;
        [SerializeField, Min(0.01f)] float duration = 1.2f;
        [SerializeField, Tooltip("度/秒。負で時計回り")] float rotationSpeed = 480f;

        public SpiralMotion() { }

        public SpiralMotion(float startRadius, float targetRadius, float duration, float rotationSpeed)
        {
            this.startRadius = startRadius;
            this.targetRadius = targetRadius;
            this.duration = duration;
            this.rotationSpeed = rotationSpeed;
        }

        public override string Summary => $"渦巻き 半径 {startRadius:0.##}→{targetRadius:0.##} / {duration:0.##}s";

        public override MotionRunner CreateRunner()
        {
            var center = Vector3.zero;
            var angle0 = 0f;
            var d = Mathf.Max(0.01f, duration);
            return new DelegatePullRunner(d, false, (r, _) =>
            {
                var radius = Mathf.Lerp(startRadius, targetRadius, Mathf.Clamp01(r.Time / d));
                var theta = angle0 + rotationSpeed * Mathf.Deg2Rad * r.Time;
                r.Target.Position = center + new Vector3(Mathf.Cos(theta), Mathf.Sin(theta), 0f) * radius;
            }, r =>
            {
                var dir = r.LaunchDirection != Vector2.zero ? r.LaunchDirection.normalized : (Vector2)(r.Target.Rotation * Vector3.right);
                center = r.Target.Position - (Vector3)dir * startRadius;
                angle0 = Mathf.Atan2(dir.y, dir.x);
            });
        }
    }
}
