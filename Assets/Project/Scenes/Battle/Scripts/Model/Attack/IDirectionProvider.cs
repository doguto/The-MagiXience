using System;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Model.Attack
{
    // TODO: 引数が増えてきたら構造体にまとめる（Player/Enemy の Position/Rotation 等を集約）
    public interface IDirectionProvider
    {
        void Initialize(Func<Vector3> getPlayerPosition, Func<Vector3> getEnemyPosition, Func<Quaternion> getEnemyRotation);
        Vector2 GetDirection();
        IDirectionProvider Clone();
    }
}
