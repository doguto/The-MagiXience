using System;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Model.Attack
{
    // 敵の正面（左向き）を親の回転に従って回した方向を返す
    [Serializable]
    public class ForwardDirectionConfig : IDirectionProvider
    {
        static readonly Vector2 Forward = Vector2.left;

        Func<Quaternion> getEnemyRotation;

        public void Initialize(Func<Vector3> getPlayerPosition, Func<Vector3> getEnemyPosition, Func<Quaternion> getEnemyRotation)
        {
            this.getEnemyRotation = getEnemyRotation;
        }

        public Vector2 GetDirection()
        {
            var rotation = getEnemyRotation != null ? getEnemyRotation() : Quaternion.identity;
            return ((Vector2)(rotation * Forward)).normalized;
        }

        public IDirectionProvider Clone() => new ForwardDirectionConfig();
    }
}
