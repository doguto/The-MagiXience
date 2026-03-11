using System;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Model.Attack
{
    [Serializable]
    public class IntervalAttackConfig : IAttackConfig
    {
        [SerializeField] float attackInterval = 2.0f;
        [SerializeReference, SubclassSelector] IDirectionProvider directionProvider;

        public IAttackStrategy CreateStrategy(IPlayerPositionProvider playerPositionProvider, Func<Vector3> getEnemyPosition)
        {
            var provider = directionProvider ?? new FixedDirectionConfig();
            provider.Initialize(playerPositionProvider, getEnemyPosition);
            return new IntervalAttackStrategy(attackInterval, provider);
        }
    }
}
