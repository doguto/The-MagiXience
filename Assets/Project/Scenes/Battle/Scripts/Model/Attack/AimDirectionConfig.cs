using System;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Model.Attack
{
    [Serializable]
    public class AimDirectionConfig : IDirectionProvider
    {
        Func<Vector3> getPlayerPosition;
        Func<Vector3> getEnemyPosition;

        public void Initialize(Func<Vector3> getPlayerPosition, Func<Vector3> getEnemyPosition, Func<Quaternion> getEnemyRotation)
        {
            this.getPlayerPosition = getPlayerPosition;
            this.getEnemyPosition = getEnemyPosition;
        }

        public Vector2 GetDirection()
        {
            var direction = ((Vector2)(getPlayerPosition() - getEnemyPosition())).normalized;
            return direction == Vector2.zero ? Vector2.left : direction;
        }

        public IDirectionProvider Clone() => new AimDirectionConfig();
    }
}
