using System;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Model.Attack
{
    [Serializable]
    public class SelfRotationDirectionConfig : IDirectionProvider
    {
        [SerializeField] Vector2 baseDirection = Vector2.left;

        Func<Quaternion> getEnemyRotation;

        public void Initialize(Func<Vector3> getPlayerPosition, Func<Vector3> getEnemyPosition, Func<Quaternion> getEnemyRotation)
        {
            this.getEnemyRotation = getEnemyRotation;
        }

        public Vector2 GetDirection()
        {
            var rotation = getEnemyRotation != null ? getEnemyRotation() : Quaternion.identity;
            var rotated = (Vector2)(rotation * baseDirection);
            return rotated == Vector2.zero ? Vector2.left : rotated.normalized;
        }

        public IDirectionProvider Clone() => new SelfRotationDirectionConfig { baseDirection = baseDirection };
    }
}
