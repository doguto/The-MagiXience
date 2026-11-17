using System;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Runtime.Motion
{
    // 旧 HomingMovementConfig
    [Serializable]
    public sealed class HomingMotion : MotionStep
    {
        [SerializeField] float speed = 5f;
        [SerializeField, Tooltip("旋回速度 (度/秒)")] float turnSpeed = 90f;
        [SerializeField, Min(0f), Tooltip("0 で無限")] float duration;

        public override string Summary => $"ホーミング {speed:0.##} / 旋回 {turnSpeed:0.#}°";

        public override MotionRunner CreateRunner()
        {
            var dir = Vector3.left;
            return new DelegatePullRunner(duration, true, (r, dt) =>
            {
                if (BattleTargets.TryGetPlayerPosition(out var player))
                {
                    var toPlayer = (Vector3)((Vector2)(player - r.Target.Position)).normalized;
                    dir = Vector3.RotateTowards(dir, toPlayer, turnSpeed * Mathf.Deg2Rad * dt, 0f);
                }
                r.Target.Position += dir * speed * dt;
            }, r => dir = r.Target.Rotation * Vector3.right);
        }
    }
}
