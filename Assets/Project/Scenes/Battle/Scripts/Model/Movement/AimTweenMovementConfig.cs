using System;
using DG.Tweening;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Model.Movement
{
    /// <summary>
    /// 移動開始時点の Player 座標を目標として TweenMovement を行う移動設定。
    /// x / y フラグで移動する軸を指定する。
    /// どちらも false のとき移動しない。
    /// 片方だけ true のとき、その軸成分のみ Player 座標に合わせる（他方は現在値を維持）。
    /// 両方 true のとき Player 座標そのものへ向かう。
    /// Ease ドロップダウンで "CustomCurve" を選ぶと AnimationCurve を使用する。
    /// </summary>
    [Serializable]
    public class AimTweenMovementConfig : IMovementStep
    {
        /// <summary>
        /// DOTween の Ease 値 + カスタムカーブ用のセンチネルを兼ねる。
        /// CustomCurve = -1 として扱い、それ以外は Ease にキャストする。
        /// </summary>
        public const int CustomCurveValue = -1;

        [SerializeField, Min(0f)] float duration = 1f;
        [SerializeField, Tooltip("x 軸方向に Player 座標へ移動する")] bool x = true;
        [SerializeField, Tooltip("y 軸方向に Player 座標へ移動する")] bool y = true;
        [SerializeField] int easeValue = (int)Ease.Linear;
        [SerializeField] AnimationCurve customCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        [SerializeField] EaseCurvePreset curvePreset;

        public Tween Play(Transform target, Vector2 direction, Animator animator)
        {
            Vector3 current = target.position;

            // どちらの軸も指定されていない場合は移動せず、duration ぶん待機する。
            // 呼び出し側が Tween の null を許容しないケースがあるため null は返さない。
            if (!x && !y)
            {
                return DOVirtual.DelayedCall(duration, null);
            }

            var playerTransform = PlayerPositionReference.Transform;

            // Player が存在しない場合は現在位置を目標にする（実質待機）。
            Vector3 playerPosition = playerTransform != null ? playerTransform.position : current;

            Vector3 destination = new Vector3(
                x ? playerPosition.x : current.x,
                y ? playerPosition.y : current.y,
                current.z);

            var tween = target.DOMove(destination, duration);

            if (easeValue == CustomCurveValue)
            {
                var curve = curvePreset != null ? curvePreset.Curve : customCurve;
                return tween.SetEase(curve);
            }

            return tween.SetEase((Ease)easeValue);
        }
    }
}
