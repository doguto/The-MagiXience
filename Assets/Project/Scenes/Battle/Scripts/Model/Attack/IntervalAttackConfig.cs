using System;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Model.Attack
{
    [Serializable]
    public class IntervalAttackConfig : IAttackConfig
    {
        [SerializeField] float attackInterval = 2.0f;
        [SerializeReference, SubclassSelector] IDirectionProvider directionProvider;

        public IAttackStrategy CreateStrategy(Func<Vector3> getPlayerPosition, Func<Vector3> getEnemyPosition)
        {
            var provider = directionProvider ?? new FixedDirectionConfig();
            provider.Initialize(getPlayerPosition, getEnemyPosition);
            return new IntervalAttackStrategy(attackInterval, provider);
        }
    }
}
