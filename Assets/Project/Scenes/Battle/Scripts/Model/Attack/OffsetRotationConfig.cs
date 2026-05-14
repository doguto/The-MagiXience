using System;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Model.Attack
{
    [Serializable]
    public class OffsetRotationConfig : IRotationProvider
    {
        [SerializeField] float offsetDegrees;

        Func<Quaternion> getEnemyRotation;

        public void Initialize(Func<Vector3> getPlayerPosition, Func<Vector3> getEnemyPosition, Func<Quaternion> getEnemyRotation)
        {
            this.getEnemyRotation = getEnemyRotation;
        }

        public Quaternion GetRotation()
        {
            var baseRotation = getEnemyRotation != null ? getEnemyRotation() : Quaternion.identity;
            return baseRotation * Quaternion.Euler(0f, 0f, offsetDegrees);
        }

        public IRotationProvider Clone() => new OffsetRotationConfig { offsetDegrees = offsetDegrees };
    }
}
