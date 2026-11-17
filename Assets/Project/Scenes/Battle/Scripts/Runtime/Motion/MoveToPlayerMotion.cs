using System;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Runtime.Motion
{
    // 旧 AimTweenMovementConfig
    [Serializable]
    public sealed class MoveToPlayerMotion : MotionStep
    {
        [SerializeField, Min(0f)] float duration = 1f;
        [SerializeField] bool x = true;
        [SerializeField] bool y = true;
        [SerializeField] MotionEase ease = MotionEase.Of(DG.Tweening.Ease.Linear);

        public override string Summary => $"自機へ ({(x ? "X" : "")}{(y ? "Y" : "")}) / {duration:0.##}s {ease}";

        public override MotionRunner CreateRunner()
        {
            if (!x && !y) return new DelegatePullRunner(Mathf.Max(0.0001f, duration), false, (_, _) => { });
            return new TweenRunner(duration, ease, b =>
            {
                var current = b.Position;
                var player = BattleTargets.TryGetPlayerPosition(out var p) ? p : current;
                return new Vector3(x ? player.x : current.x, y ? player.y : current.y, current.z);
            });
        }
    }
}
