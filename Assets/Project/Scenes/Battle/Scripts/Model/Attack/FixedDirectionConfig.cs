using System;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Model.Attack
{
    [Serializable]
    public class FixedDirectionConfig : IDirectionProvider
    {
        [SerializeField] Vector2 direction = Vector2.left;

        public void Initialize(Func<Vector3> getPlayerPosition, Func<Vector3> getEnemyPosition) { }

        public Vector2 GetDirection() => direction.normalized;
    }
}
