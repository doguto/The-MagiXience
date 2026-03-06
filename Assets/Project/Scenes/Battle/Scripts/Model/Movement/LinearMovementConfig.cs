using System;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Model.Movement
{
    [Serializable]
    public class LinearMovementConfig : IMovementConfig
    {
        [SerializeField] Vector3 velocity = Vector3.left;

        public IMovementStrategy CreateStrategy()
        {
            return new LinearMovement(velocity);
        }

        public IMovementStrategy CreateStrategy(Vector2 direction)
        {
            float speed = velocity.magnitude;
            return new LinearMovement((Vector3)direction.normalized * speed);
        }
    }
}
