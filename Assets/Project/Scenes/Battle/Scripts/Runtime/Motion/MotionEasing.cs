using DG.Tweening;
using DG.Tweening.Core.Easing;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Runtime.Motion
{
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
