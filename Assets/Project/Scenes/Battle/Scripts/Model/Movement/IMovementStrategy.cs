using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Model.Movement
{
    public interface IMovementStrategy
    {
        Vector3 UpdateMovement(Vector3 currentPosition, float deltaTime);

        void Initialize();
        bool IsCompleted { get; }
    }
}
