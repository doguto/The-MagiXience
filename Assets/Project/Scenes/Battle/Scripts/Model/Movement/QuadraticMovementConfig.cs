using System;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Model.Movement
{
    [Serializable]
    public class QuadraticMovementConfig : IMovementConfig
    {
        [SerializeField] Vector3 initialVelocity = Vector3.left;
        [SerializeField] Vector3 acceleration = Vector3.down;

        public IMovementStrategy CreateStrategy()
        {
            return new QuadraticMovement(initialVelocity, acceleration);
        }

        public IMovementStrategy CreateStrategy(Vector2 direction)
        {
            return CreateStrategy();
        }
    }
}
