using System;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Model.Attack
{
    public interface IAttackConfig
    {
        IAttackStrategy CreateStrategy(Func<Vector3> getPlayerPosition, Func<Vector3> getEnemyPosition);
    }
}
