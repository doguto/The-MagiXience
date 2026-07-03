using System;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Model.Attack
{
    [Serializable]
    public class InheritParentRotationConfig : IRotationProvider
    {
        Func<Quaternion> getEnemyRotation;

        public void Initialize(Func<Vector3> getPlayerPosition, Func<Vector3> getEnemyPosition, Func<Quaternion> getEnemyRotation)
        {
            this.getEnemyRotation = getEnemyRotation;
        }

        public Quaternion GetRotation() => getEnemyRotation != null ? getEnemyRotation() : Quaternion.identity;

        public IRotationProvider Clone() => new InheritParentRotationConfig();
    }
}
