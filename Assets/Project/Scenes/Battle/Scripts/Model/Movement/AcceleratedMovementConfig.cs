using System;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Model.Movement
{
    [Serializable]
    public class AcceleratedMovementConfig : IMovementConfig
    {
        [SerializeField] Vector2 direction = Vector2.left;
        [SerializeField] float initialSpeed = 2f;
        [SerializeField] float acceleration = 1f;
        [SerializeField] float maxSpeed = 10f;

        public IMovementStrategy CreateStrategy()
        {
            return CreateStrategy((Vector2)direction);
        }

        public IMovementStrategy CreateStrategy(Vector2 overrideDirection)
        {
            Vector3 dir = overrideDirection.normalized;
            Vector3 initialVelocity = dir * initialSpeed;
            Vector3 accel = dir * acceleration;
            return new AcceleratedMovement(initialVelocity, accel, maxSpeed);
        }
    }
}
