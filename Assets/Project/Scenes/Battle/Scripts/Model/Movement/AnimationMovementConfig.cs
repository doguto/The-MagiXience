using System;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Model.Movement
{
    /// <summary>
    /// AnimationMovement 用の Config。
    /// clip を Inspector で指定し、Initialize() のタイミングで AnimatorOverrideController を使って動的に差し替える。
    /// SequentialMovementConfig のステップとして配置する。
    /// </summary>
    [Serializable]
    public class AnimationMovementConfig : IMovementConfig
    {
        [SerializeField] AnimationClip clip;
        [SerializeField] int layerIndex = 0;

        /// <summary>Presenter 側から Animator を渡して Strategy を生成する。</summary>
        public IMovementStrategy CreateStrategy(Animator animator)
        {
            if (clip == null)
            {
                Debug.LogWarning("[AnimationMovementConfig] AnimationClip が設定されていません。StaticMovement にフォールバックします。");
                return new StaticMovement();
            }
            return new AnimationMovement(animator, clip, layerIndex);
        }

        // IMovementConfig の実装（Animatorなしでは動かないのでフォールバックとして StaticMovement）
        public IMovementStrategy CreateStrategy()
        {
            Debug.LogWarning("[AnimationMovementConfig] Animator が注入されていません。StaticMovement にフォールバックします。");
            return new StaticMovement();
        }

        public IMovementStrategy CreateStrategy(Vector2 direction)
        {
            return CreateStrategy();
        }
    }
}
