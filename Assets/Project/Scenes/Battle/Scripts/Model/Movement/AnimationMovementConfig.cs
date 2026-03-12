using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Model.Movement
{
    /// <summary>
    /// AnimationClip を再生し、クリップ終了まで待機する移動ステップ。
    /// Sequence の前のステップが終わったタイミングで再生を開始する。
    /// </summary>
    [Serializable]
    public class AnimationMovementConfig : IMovementStep
    {
        [SerializeField] AnimationClip clip;
        [SerializeField] int layerIndex = 0;

        public Tween Play(Transform target, Vector2 direction, Animator animator)
        {
            if (animator == null || clip == null)
            {
                Debug.LogWarning("[AnimationMovementConfig] Animator または AnimationClip が未設定です。");
                return DOVirtual.DelayedCall(0f, () => { });
            }

            // OverrideController のセットアップだけ先に済ませておく（再生はしない）
            var overrideController = new AnimatorOverrideController(animator.runtimeAnimatorController);
            var overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>(1);
            overrideController.GetOverrides(overrides);
            overrides[0] = new KeyValuePair<AnimationClip, AnimationClip>(overrides[0].Key, clip);
            overrideController.ApplyOverrides(overrides);
            animator.runtimeAnimatorController = overrideController;

            // OnStart（このステップが実際に始まるタイミング）で再生を開始する
            return DOVirtual.DelayedCall(clip.length, () => { })
                .OnStart(() => animator.Play("Base", layerIndex, 0f));
        }
    }
}
