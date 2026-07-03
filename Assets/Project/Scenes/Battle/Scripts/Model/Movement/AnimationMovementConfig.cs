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

            // OverrideController のセットアップも含めて OnStart に移す。
            // runtimeAnimatorController への代入だけで Unity がステートをリセット・再生してしまうため。
            return DOVirtual.DelayedCall(clip.length, () => { })
                .OnStart(() =>
                {
                    var overrideController = new AnimatorOverrideController(animator.runtimeAnimatorController);
                    var overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>(1);
                    overrideController.GetOverrides(overrides);
                    overrides[0] = new KeyValuePair<AnimationClip, AnimationClip>(overrides[0].Key, clip);
                    overrideController.ApplyOverrides(overrides);
                    animator.runtimeAnimatorController = overrideController;
                    animator.Play("Base", layerIndex, 0f);
                });
        }
    }
}
