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
    }
}
