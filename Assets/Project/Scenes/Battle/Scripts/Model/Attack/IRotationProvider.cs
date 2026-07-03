using System;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Model.Attack
{
    public interface IRotationProvider
    {
        void Initialize(Func<Vector3> getPlayerPosition, Func<Vector3> getEnemyPosition, Func<Quaternion> getEnemyRotation);
        Quaternion GetRotation();
        IRotationProvider Clone();
    }
}
