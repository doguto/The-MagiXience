using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Model.Movement
{
    public class StaticMovement : IMovementStrategy
    {
        public Vector3 UpdateMovement(Vector3 currentPosition, float deltaTime)
        {
            return currentPosition; // 位置を変更しない
        }

        public void Initialize()
        {
            // 初期化不要
        }

        public bool IsCompleted => false;
    }
}
