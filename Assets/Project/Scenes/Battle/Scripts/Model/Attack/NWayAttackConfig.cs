using System;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Model.Attack
{
    [Serializable]
    public class NWayAttackConfig : IAttackConfig
    {
        [SerializeField] float attackInterval = 2.0f;
        [SerializeField] int wayCount = 3;
        [SerializeField] float spreadAngle = 60f;
        [SerializeField] Vector2 baseDirection = Vector2.left;

        public IAttackStrategy CreateStrategy(IPlayerPositionProvider playerPositionProvider)
        {
            return new NWayAttackStrategy(attackInterval, wayCount, spreadAngle, baseDirection);
        }
    }
}
