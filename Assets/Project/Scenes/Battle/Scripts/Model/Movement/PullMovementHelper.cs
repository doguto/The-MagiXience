using DG.Tweening;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Model.Movement
{
    /// <summary>
    /// pull 型の IMovementStrategy を DOVirtual でラップして Tween に変換するヘルパー。
    /// duration = 0 のとき、事実上無限に動き続ける Tween を返す。
    /// deltaTime には DOTween が管理する elapsed の差分を使うことで
    /// DOTween のタイムスケール設定に完全追従する。
    /// </summary>
    internal static class PullMovementHelper
    {
        // 無限継続の代替として使う十分大きな秒数（約 11.5 日）
        const float InfiniteDuration = 999999f;

        public static Tween Wrap(Transform target, IMovementStrategy strategy, float duration)
        {
            strategy.Initialize();
            float prevElapsed = 0f;
            float actualDuration = duration <= 0f ? InfiniteDuration : duration;

            return DOVirtual.Float(0f, actualDuration, actualDuration, elapsed =>
            {
                float delta = elapsed - prevElapsed;
                prevElapsed = elapsed;
                target.position = strategy.UpdateMovement(target.position, delta);
            });
        }
    }
}
