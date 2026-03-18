using System;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Model.Attack
{
    [Serializable]
    public class IntervalAttackConfig : IAttackConfig
    {
        [SerializeField] float attackInterval = 2.0f;
        public IAttackStrategy CreateStrategy()
        {
            return new IntervalAttackStrategy(attackInterval);
        }
    }
}
