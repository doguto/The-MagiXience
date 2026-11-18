using System;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Runtime.Motion
{
    // 旧 StraightWithHomingMovementConfig。常に左へ進みつつ Y を自機に寄せる
    [Serializable]
    public sealed class StraightWithHomingMotion : MotionStep
    {
        [SerializeField] float speed = 5f;
        [SerializeField] float verticalTrackingSpeed = 2f;
        [SerializeField, Min(0f), Tooltip("0 で無限")] float duration;

        public override string Summary => $"左進 {speed:0.##} / 縦追尾 {verticalTrackingSpeed:0.##}";

        public override MotionRunner CreateRunner() => new DelegatePullRunner(duration, true, (r, dt) =>
        {
            var p = r.Target.Position;
            p.x -= speed * dt;
            if (BattleTargets.TryGetPlayerPosition(out var player))
                p.y = Mathf.MoveTowards(p.y, player.y, verticalTrackingSpeed * dt);
            r.Target.Position = p;
        });
    }
}
