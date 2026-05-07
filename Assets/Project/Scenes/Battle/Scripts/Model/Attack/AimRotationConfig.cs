using System;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Model.Attack
{
    [Serializable]
    public class AimRotationConfig : IRotationProvider
    {
        Func<Vector3> getPlayerPosition;
        Func<Vector3> getEnemyPosition;

        public void Initialize(Func<Vector3> getPlayerPosition, Func<Vector3> getEnemyPosition, Func<Quaternion> getEnemyRotation)
        {
            this.getPlayerPosition = getPlayerPosition;
            this.getEnemyPosition = getEnemyPosition;
        }

        public Quaternion GetRotation()
        {
            var toPlayer = (Vector2)(getPlayerPosition() - getEnemyPosition());
            if (toPlayer == Vector2.zero) return Quaternion.identity;
            // 敵の正面は左向きで固定
            return Quaternion.FromToRotation(Vector2.left, toPlayer);
        }

        public IRotationProvider Clone() => new AimRotationConfig();
    }
}
