using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Model.Movement
{
    /// <summary>
    /// Animatorのクリップ再生中は位置を動かさず、クリップ終了で IsCompleted = true になる。
    /// SequentialMovement のステップとして使う。
    /// Initialize() のタイミングで AnimationClip を AnimatorOverrideController で差し替えて再生する。
    /// </summary>
    public class AnimationMovement : IMovementStrategy
    {
        readonly Animator animator;
        readonly AnimationClip clip;
        readonly int layerIndex;
        bool started;

        public bool IsCompleted
        {
            get
            {
                if (!started) return false;
                var state = animator.GetCurrentAnimatorStateInfo(layerIndex);
                // normalizedTime >= 1 かつ遷移中でないことを確認
                return !animator.IsInTransition(layerIndex) && state.normalizedTime >= 1f;
            }
        }

        /// <param name="animator">監視対象の Animator</param>
        /// <param name="clip">再生する AnimationClip</param>
        /// <param name="layerIndex">監視する Animator レイヤー（通常は 0）</param>
        public AnimationMovement(Animator animator, AnimationClip clip, int layerIndex = 0)
        {
            this.animator = animator;
            this.clip = clip;
            this.layerIndex = layerIndex;
        }

        public void Initialize()
        {
            started = true;

            // AnimatorOverrideController でテンプレートの唯一のクリップを差し替えて再生
            var overrideController = new AnimatorOverrideController(animator.runtimeAnimatorController);
            var overrides = new System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<AnimationClip, AnimationClip>>(1);
            overrideController.GetOverrides(overrides);
            overrides[0] = new System.Collections.Generic.KeyValuePair<AnimationClip, AnimationClip>(overrides[0].Key, clip);
            overrideController.ApplyOverrides(overrides);

            animator.runtimeAnimatorController = overrideController;
            animator.Play("Base", layerIndex, 0f);
        }

        public Vector3 UpdateMovement(Vector3 currentPosition, float deltaTime)
        {
            // 位置は動かさない。Animator が Transform を動かす。
            return currentPosition;
        }
    }
}
