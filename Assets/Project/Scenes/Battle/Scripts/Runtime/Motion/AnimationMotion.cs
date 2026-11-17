using System;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Runtime.Motion
{
    // 旧 AnimationMovementConfig。Animator にクリップを再生させ、その長さだけ待つ
    [Serializable]
    public sealed class AnimationMotion : MotionStep
    {
        [SerializeField] AnimationClip clip;
        [SerializeField] int layerIndex;

        public override string Summary => clip ? $"アニメ {clip.name}" : "アニメ (未設定)";

        public override MotionRunner CreateRunner()
        {
            var length = clip ? clip.length : 0f;
            return new DelegatePullRunner(Mathf.Max(0.0001f, length), false, (_, _) => { }, r =>
            {
                var animator = r.Target.Transform ? r.Target.Transform.GetComponent<Animator>() : null;
                if (animator == null || clip == null)
                {
                    Debug.LogWarning("[AnimationMotion] Animator or clip is missing.");
                    return;
                }
                var overrideController = new AnimatorOverrideController(animator.runtimeAnimatorController);
                var overrides = new System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<AnimationClip, AnimationClip>>();
                overrideController.GetOverrides(overrides);
                if (overrides.Count > 0) overrides[0] = new(overrides[0].Key, clip);
                overrideController.ApplyOverrides(overrides);
                animator.runtimeAnimatorController = overrideController;
                animator.Play("Base", layerIndex, 0f);
            });
        }
    }
}
