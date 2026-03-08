using System.Collections.Generic;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Model.Movement
{
    /// <summary>
    /// 複数のMovementStrategyを順番に実行する。
    /// 各ステップの IsCompleted が true になったら次のステップへ進む。
    /// 全ステップ完了後は最後のStrategyをそのまま使い続ける。
    /// </summary>
    public class SequentialMovement : IMovementStrategy
    {
        readonly IReadOnlyList<IMovementStrategy> steps;
        int currentIndex;

        public SequentialMovement(IReadOnlyList<IMovementStrategy> steps)
        {
            this.steps = steps;
        }

        IMovementStrategy Current => steps[currentIndex];

        public bool IsCompleted => currentIndex >= steps.Count - 1 && Current.IsCompleted;

        public void Initialize()
        {
            currentIndex = 0;
            Current.Initialize();
        }

        public Vector3 UpdateMovement(Vector3 currentPosition, float deltaTime)
        {
            // 現在のステップが完了していたら次へ進む
            while (Current.IsCompleted && currentIndex < steps.Count - 1)
            {
                currentIndex++;
                Current.Initialize();
            }

            return Current.UpdateMovement(currentPosition, deltaTime);
        }
    }
}
