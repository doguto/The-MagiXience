using System;
using DG.Tweening;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Model.Movement
{
    /// <summary>
    /// 毎フレーム呼ばれるデルタタイムベースの計算を DOVirtual でラップして Tween に変換するヘルパー。
    /// duration = 0 のとき無限に動き続ける Tween を返す。
    /// deltaTime には DOTween が管理する elapsed の差分を使うことで
    /// DOTween のタイムスケール設定に完全追従する。
    /// </summary>
    internal static class PullMovementHelper
    {
        const float InfiniteDuration = 999999f;

        /// <summary>
        /// onUpdate(target, deltaTime) を毎フレーム呼ぶ Tween を返す。
        /// </summary>
        public static Tween Create(Transform target, float duration, Action<Transform, float> onUpdate)
        {
            float prevElapsed = 0f;
            float actualDuration = duration <= 0f ? InfiniteDuration : duration;

            return DOVirtual.Float(0f, actualDuration, actualDuration, elapsed =>
            {
                float delta = elapsed - prevElapsed;
                prevElapsed = elapsed;
                onUpdate(target, delta);
            });
        }
    }
}
