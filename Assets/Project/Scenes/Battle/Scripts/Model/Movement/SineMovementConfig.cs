using System;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Model.Movement
{
    [Serializable]
    public class SineMovementConfig : IMovementConfig
    {
        [SerializeField] Vector3 baseVelocity = Vector3.left;
        [SerializeField, Min(0f)] float amplitude = 1f;
        [SerializeField, Min(0.01f)] float frequency = 1f;

        public IMovementStrategy CreateStrategy()
        {
            return new SineMovement(baseVelocity, amplitude, frequency);
        }

        public IMovementStrategy CreateStrategy(Vector2 direction)
        {
            return CreateStrategy();
        }
    }
}
