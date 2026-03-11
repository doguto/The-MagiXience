using System;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Model.Attack
{
    public interface IDirectionProvider
    {
        void Initialize(IPlayerPositionProvider playerPositionProvider, Func<Vector3> getEnemyPosition);
        Vector2 GetDirection();
    }
}
