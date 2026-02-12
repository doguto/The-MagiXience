using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Model.Movement
{
    public class LinearMovement : IMovementStrategy
    {
        readonly Vector3 velocity;

        public LinearMovement(Vector3 velocity)
        {
            this.velocity = velocity;
        }

        public Vector3 UpdateMovement(Vector3 currentPosition, float deltaTime)
        {
            return currentPosition + velocity * deltaTime;
        }

        public void Initialize()
        {
            // 初期化不要
        }

        public bool IsCompleted => false; // 直線移動は終了しない
    }
}
