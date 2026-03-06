using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Model.Movement
{
    public class AcceleratedMovement : IMovementStrategy
    {
        Vector3 velocity;
        readonly Vector3 acceleration;
        readonly float maxSpeed;

        public bool IsCompleted => false;

        public AcceleratedMovement(Vector3 initialVelocity, Vector3 acceleration, float maxSpeed)
        {
            this.velocity = initialVelocity;
            this.acceleration = acceleration;
            this.maxSpeed = maxSpeed;
        }

        public void Initialize() { }

        public Vector3 UpdateMovement(Vector3 currentPosition, float deltaTime)
        {
            velocity += acceleration * deltaTime;
            if (maxSpeed > 0f) velocity = Vector3.ClampMagnitude(velocity, maxSpeed);
            return currentPosition + velocity * deltaTime;
        }
    }
}
