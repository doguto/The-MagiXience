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
        [SerializeReference, SubclassSelector] IDirectionProvider directionProvider;

        public IAttackStrategy CreateStrategy(IPlayerPositionProvider playerPositionProvider, Func<Vector3> getEnemyPosition)
        {
            var provider = directionProvider ?? new FixedDirectionConfig();
            provider.Initialize(playerPositionProvider, getEnemyPosition);
            return new NWayAttackStrategy(attackInterval, wayCount, spreadAngle, provider);
        }
    }
}
