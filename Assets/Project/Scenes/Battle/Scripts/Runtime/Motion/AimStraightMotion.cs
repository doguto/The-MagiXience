using System;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Runtime.Motion
{
    // 旧 AimStraightMovementConfig
    [Serializable]
    public sealed class AimStraightMotion : MotionStep
    {
        [SerializeField] float speed = 5f;
        [SerializeField, Min(0f), Tooltip("0 で無限")] float duration;

        public override string Summary => $"自機狙い直進 {speed:0.##}";

        public override MotionRunner CreateRunner()
        {
            var v = Vector3.zero;
            return new DelegatePullRunner(duration, true, (r, dt) => r.Target.Position += v * dt, r =>
            {
                var pos = r.Target.Position;
                v = BattleTargets.TryGetPlayerPosition(out var player)
                    ? ((Vector3)((Vector2)(player - pos)).normalized) * speed
                    : Vector3.left * speed;
            });
        }
    }
}
