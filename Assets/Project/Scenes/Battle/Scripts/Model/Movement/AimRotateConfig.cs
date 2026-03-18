using System;
using DG.Tweening;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Model.Movement
{
    /// <summary>
    /// プレイヤーの方向へ回転する移動ステップ。位置は変化しない。
    /// duration で回転にかける時間を指定する（0 の場合は即座に向く）。
    /// スプライトの初期回転をオフセットとして保持し、見た目が正しくプレイヤーを向くようにする。
    /// 後続の ForwardMovementConfig は transform.right で進むため、
    /// ここでは「プレイヤー方向角 + 初期オフセット」を最終回転として設定する。
    /// </summary>
    [Serializable]
    public class AimRotateConfig : IMovementStep
    {
        [SerializeField, Min(0f), Tooltip("回転にかける時間（秒）。0で即座に向く。")]
        float duration = 0.3f;

        [SerializeField] Ease ease = Ease.InOutSine;

        public Tween Play(Transform target, Vector2 overrideDirection, Animator animator)
        {
            Quaternion startRotation = Quaternion.identity;
            Quaternion endRotation = Quaternion.identity;
            float actualDuration = Mathf.Max(duration, 0.001f);

            return DOVirtual.Float(0f, 1f, actualDuration, t =>
            {
                target.rotation = Quaternion.SlerpUnclamped(startRotation, endRotation, t);
            })
            .OnStart(() =>
            {
                startRotation = target.rotation;
                endRotation = GetTargetRotation(target, startRotation);
            })
            .SetEase(ease);
        }

        static Quaternion GetTargetRotation(Transform target, Quaternion initialRotation)
        {
            var playerTransform = PlayerPositionReference.Transform;
            if (playerTransform == null) return initialRotation;

            Vector2 dir = (playerTransform.position - target.position).normalized;
            float aimAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            // transform.right が aimAngle 方向を指すようにする。
            // Z回転 = aimAngle のとき transform.right = (cos(aimAngle), sin(aimAngle))。
            return Quaternion.Euler(0f, 0f, aimAngle);
        }
    }
}
