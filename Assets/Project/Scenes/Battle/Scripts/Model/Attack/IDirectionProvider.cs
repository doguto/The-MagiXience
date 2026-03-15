using System;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Model.Attack
{
    public interface IDirectionProvider
    {
        void Initialize(Func<Vector3> getPlayerPosition, Func<Vector3> getEnemyPosition);
        Vector2 GetDirection();
        IDirectionProvider Clone();
    }
}
