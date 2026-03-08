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
                strategies.Add(CreateStrategyFromStep(step));
            }
            return new SequentialMovement(strategies);
        }

        IMovementStrategy CreateStrategyFromStep(IMovementConfig step)
        {
            if (injectedAnimator != null)
            {
                if (step is AnimationMovementConfig animConfig)
                    return animConfig.CreateStrategy(injectedAnimator);
                if (step is TimedMovementConfig timedConfig)
                    return timedConfig.CreateStrategy(injectedAnimator);
            }
            return step?.CreateStrategy() ?? new StaticMovement();
        }

        public IMovementStrategy CreateStrategy(Vector2 direction)
        {
            return CreateStrategy();
        }
    }
}
