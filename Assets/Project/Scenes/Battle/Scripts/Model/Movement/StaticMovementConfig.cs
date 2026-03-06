using System;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Model.Movement
{
    [Serializable]
    public class StaticMovementConfig : IMovementConfig
    {
        public IMovementStrategy CreateStrategy()
        {
            return new StaticMovement();
        }

        public IMovementStrategy CreateStrategy(Vector2 direction)
        {
            return new StaticMovement();
        }
    }
}
