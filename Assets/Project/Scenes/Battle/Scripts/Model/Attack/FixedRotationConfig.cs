using System;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Model.Attack
{
    [Serializable]
    public class FixedRotationConfig : IRotationProvider
    {
        [SerializeField] float degrees;

        public void Initialize(Func<Vector3> getPlayerPosition, Func<Vector3> getEnemyPosition, Func<Quaternion> getEnemyRotation) { }

        public Quaternion GetRotation() => Quaternion.Euler(0f, 0f, degrees);

        public IRotationProvider Clone() => new FixedRotationConfig { degrees = degrees };
    }
}
