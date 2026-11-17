using System;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Runtime.Motion
{
    // 旧 AimRotateConfig。自機の方向へ回転する (位置は動かない)
    [Serializable]
    public sealed class AimRotateMotion : MotionStep
    {
        [SerializeField, Min(0f)] float duration = 0.5f;
        [SerializeField] DG.Tweening.Ease ease = DG.Tweening.Ease.InOutSine;

        public override string Summary => $"自機へ向く {duration:0.##}s";

        public override MotionRunner CreateRunner()
        {
            var start = Quaternion.identity;
            var end = Quaternion.identity;
            var d = Mathf.Max(0.001f, duration);
            return new DelegatePullRunner(d, false, (r, _) =>
            {
                r.Target.Rotation = Quaternion.SlerpUnclamped(start, end, MotionEasing.Evaluate(ease, r.Time / d));
            }, r =>
            {
                start = r.Target.Rotation;
                end = start;
                if (BattleTargets.TryGetPlayerPosition(out var player))
                {
                    var to = (Vector2)(player - r.Target.Position);
                    end = Quaternion.Euler(0f, 0f, Mathf.Atan2(to.y, to.x) * Mathf.Rad2Deg);
                }
            });
        }
    }
}
