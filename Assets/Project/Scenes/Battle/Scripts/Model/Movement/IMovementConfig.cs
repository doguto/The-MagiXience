using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Model.Movement
{
    public interface IMovementConfig
    {
        IMovementStrategy CreateStrategy();
        IMovementStrategy CreateStrategy(Vector2 direction);
    }
}
