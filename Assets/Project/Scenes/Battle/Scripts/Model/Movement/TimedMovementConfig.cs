using System;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Model.Movement
{
    /// <summary>
    /// 任意の IMovementConfig に時間制限を付けるラッパー Config。
    /// SequentialMovementConfig のステップとして使う。
    /// </summary>
    [Serializable]
    public class TimedMovementConfig : IMovementConfig
    {
        [SerializeField, Min(0.01f)] float duration = 1f;
        [SerializeReference, SubclassSelector]
        IMovementConfig inner = new LinearMovementConfig();

        public IMovementStrategy CreateStrategy()
        {
            var innerStrategy = inner?.CreateStrategy() ?? new StaticMovement();
            return new TimedMovement(innerStrategy, duration);
        }

        public IMovementStrategy CreateStrategy(Vector2 direction)
        {
            var innerStrategy = inner?.CreateStrategy(direction) ?? new StaticMovement();
            return new TimedMovement(innerStrategy, duration);
        }

        public IMovementStrategy CreateStrategy(Animator animator)
        {
            IMovementStrategy innerStrategy = inner switch
            {
                AnimationMovementConfig animConfig => animConfig.CreateStrategy(animator),
                _ => inner?.CreateStrategy() ?? new StaticMovement()
            };
            return new TimedMovement(innerStrategy, duration);
        }
    }
}
