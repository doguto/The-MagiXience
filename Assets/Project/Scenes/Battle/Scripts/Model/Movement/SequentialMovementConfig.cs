using System;
using System.Collections.Generic;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Model.Movement
{
    [Serializable]
    public class SequentialMovementConfig : IMovementConfig
    {
        [SerializeReference, SubclassSelector]
        List<IMovementConfig> steps = new();

        Animator injectedAnimator;

        /// <summary>
        /// AnimationMovementConfig を含む場合、Presenter 側からこのメソッドで Animator を注入する。
        /// CreateStrategy() より前に呼ぶこと。
        /// </summary>
        public void InjectAnimator(Animator animator)
        {
            injectedAnimator = animator;
        }

        public IMovementStrategy CreateStrategy()
        {
            var strategies = new List<IMovementStrategy>(steps.Count);
            foreach (var step in steps)
            {
                if (step is AnimationMovementConfig animConfig && injectedAnimator != null)
                    strategies.Add(animConfig.CreateStrategy(injectedAnimator));
                else
                    strategies.Add(step?.CreateStrategy() ?? new StaticMovement());
            }
            return new SequentialMovement(strategies);
        }

        public IMovementStrategy CreateStrategy(Vector2 direction)
        {
            return CreateStrategy();
        }
    }
}
