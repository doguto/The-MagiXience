using System;
using DG.Tweening;
using DG.Tweening.Core.Easing;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Runtime.Motion
{
    // 旧設定と同じ表現の Ease 指定。easeValue = -1 でカスタムカーブ、それ以外は DG.Tweening.Ease。
    [Serializable]
    public struct MotionEase
    {
        public const int CustomCurveValue = -1;

        public int easeValue;
        public AnimationCurve customCurve;
        public EaseCurveAsset curvePreset;

        public static MotionEase Of(Ease ease) => new() { easeValue = (int)ease };

        public float Evaluate(float t)
        {
            t = Mathf.Clamp01(t);
            if (easeValue == CustomCurveValue)
            {
                var curve = curvePreset != null ? curvePreset.Curve : customCurve;
                if (curve == null || curve.length == 0) return t;
                var end = curve.keys[curve.length - 1].time;
                return curve.Evaluate(t * end);
            }
            return MotionEasing.Evaluate((Ease)easeValue, t);
        }

        public override string ToString() => easeValue == CustomCurveValue ? "Curve" : ((Ease)easeValue).ToString();
    }

    public static class MotionEasing
    {
        // DOTween の既定 Ease (Unset のとき適用される)
        public const Ease DefaultEase = Ease.OutQuad;

        public static float Evaluate(Ease ease, float t)
        {
            if (ease == Ease.Unset) ease = DefaultEase;
            return EaseManager.Evaluate(ease, null, Mathf.Clamp01(t), 1f, 1.70158f, 0f);
        }
    }
}
