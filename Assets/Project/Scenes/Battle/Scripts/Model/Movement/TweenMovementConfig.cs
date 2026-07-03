using System;
using DG.Tweening;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Model.Movement
{
    /// <summary>
    /// DOTween ネイティブの移動設定。
    /// Inspector で targetOffset / duration / Ease を直接指定できる。
    /// isRelative = true のとき targetOffset は現在位置からの相対移動。
    /// isRelative = false のとき targetOffset はワールド座標の絶対位置。
    /// Ease ドロップダウンで "CustomCurve" を選ぶと AnimationCurve を使用する。
    /// </summary>
    [Serializable]
    public class TweenMovementConfig : IMovementStep
    {
        /// <summary>
        /// DOTween の Ease 値 + カスタムカーブ用のセンチネルを兼ねる。
        /// CustomCurve = -1 として扱い、それ以外は Ease にキャストする。
        /// </summary>
        public const int CustomCurveValue = -1;

        [SerializeField] Vector3 targetOffset = new Vector3(-3f, 0f, 0f);
        [SerializeField, Min(0f)] float duration = 1f;
        [SerializeField] int easeValue = (int)Ease.Linear;
        [SerializeField] AnimationCurve customCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        [SerializeField] EaseCurvePreset curvePreset;
        [SerializeField] bool isRelative = true;

        public Tween Play(Transform target, Vector2 direction, Animator animator)
        {
            Vector3 destination = isRelative ? target.position + targetOffset : targetOffset;
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
