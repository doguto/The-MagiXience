using System;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Model.Attack
{
    [Serializable]
    public class AimDirectionConfig : IDirectionProvider
    {
        IPlayerPositionProvider playerPositionProvider;
        Func<Vector3> getEnemyPosition;

        public void Initialize(IPlayerPositionProvider playerPositionProvider, Func<Vector3> getEnemyPosition)
        {
            this.playerPositionProvider = playerPositionProvider;
            this.getEnemyPosition = getEnemyPosition;
        }

        public Vector2 GetDirection()
        {
            var enemyPos = getEnemyPosition();
            var playerPos = playerPositionProvider.PlayerPosition;
            var direction = ((Vector2)(playerPos - enemyPos)).normalized;
            return direction == Vector2.zero ? Vector2.left : direction;
        }
    }
}
