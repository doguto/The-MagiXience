using System;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Model.Attack
{
    public interface IAttackConfig
    {
        IAttackStrategy CreateStrategy(IPlayerPositionProvider playerPositionProvider, Func<Vector3> getEnemyPosition);
    }
}
