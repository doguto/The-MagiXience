using System;
using DG.Tweening;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Model.Movement
{
    /// <summary>定位置を基準に、予備動作の後に正弦波で往復する。無期限ならフェーズをまたいで継続する。</summary>
    [Serializable]
    public class AnchoredSineMovementConfig : IMovementStep, IPhaseContinuousMovementStep
    {
        [SerializeField] Vector3 anchor = new(5.7f, 0f, 0f);
        [SerializeField] Vector3 amplitude = new(0f, 1.4f, 0f);
        [SerializeField, Tooltip("角速度（ラジアン/秒）。周期は2π÷この値。")]
        float angularSpeed = 0.65f;
        [SerializeField, Min(0f)] float cueSeconds = 0.9f;
        [SerializeField] Vector3 cueOffset = new(0f, 0.18f, 0f);
        [SerializeField, Min(0f), Tooltip("0ならフェーズが終わるまで継続。")]
        float duration;

        public bool ContinueAcrossPhases => duration <= 0f;

        public Vector3 PositionAt(float elapsed)
        {
            if (cueSeconds > 0f && elapsed < cueSeconds)
                return anchor + cueOffset * Mathf.Sin(Mathf.PI * elapsed / cueSeconds);
            return anchor + amplitude * Mathf.Sin((elapsed - cueSeconds) * angularSpeed);
        }

        public Tween Play(Transform target, Vector2 direction, Animator animator)
        {
            float elapsed = 0f;
            return PullMovementHelper.Create(target, duration, (t, dt) =>
            {
                elapsed += dt;
                t.position = PositionAt(elapsed);
            }, Ease.Linear);
        }
    }
}
