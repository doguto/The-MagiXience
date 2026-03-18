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
        public static Tween Create(
            Transform target, float duration,
            Action<Transform, float> onUpdate,
            Ease ease = Ease.Unset)
        {
            return Create(target, duration, null, onUpdate, ease);
        }

        /// <summary>
        /// Tween の初回フレームで onStart を呼び、以降は onUpdate を毎フレーム呼ぶ Tween を返す。
        /// onStart は Sequence 内で前のステップが完了した後に呼ばれるため、
        /// その時点の target.position を使った初期化処理に適している。
        /// ease に Ease.Linear を指定すると dt が実時間と一致する。
        /// </summary>
        public static Tween Create(
            Transform target, float duration,
            Action<Transform> onStart,
            Action<Transform, float> onUpdate,
            Ease ease = Ease.Unset)
        {
            float prevElapsed = 0f;
            float actualDuration = duration <= 0f ? InfiniteDuration : duration;
            bool initialized = false;

            var tween = DOVirtual.Float(0f, actualDuration, actualDuration, elapsed =>
            {
                if (!initialized)
                {
                    initialized = true;
                    onStart?.Invoke(target);
                }

                float delta = elapsed - prevElapsed;
                prevElapsed = elapsed;
                onUpdate(target, delta);
            });

            if (ease != Ease.Unset)
                tween.SetEase(ease);

            return tween;
        }
    }
}
