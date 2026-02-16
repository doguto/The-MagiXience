using System;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Model.Attack
{
    public class IntervalAttackStrategy : IAttackStrategy
    {
        readonly float attackInterval;
        readonly Action onAttackCallback;

        float timeSinceLastAttack;

        public IntervalAttackStrategy(float attackInterval, Action onAttackCallback)
        {
            this.attackInterval = attackInterval;
            this.onAttackCallback = onAttackCallback;
        }

        public bool CanAttack { get; private set; }

        public void Initialize()
        {
            timeSinceLastAttack = 0;
            CanAttack = false;
        }

        public void Update(float deltaTime)
        {
            timeSinceLastAttack += deltaTime;
            if (timeSinceLastAttack >= attackInterval)
            {
                CanAttack = true;
                timeSinceLastAttack = 0;
                onAttackCallback?.Invoke();
            }
            else
            {
                CanAttack = false;
            }
        }
    }
}
