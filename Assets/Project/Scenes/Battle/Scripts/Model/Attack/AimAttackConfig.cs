using System;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Model.Attack
{
    [Serializable]
    public class AimAttackConfig : IAttackConfig
    {
        [SerializeField] float attackInterval = 2.0f;

        // getEnemyPosition は CreateStrategy 呼び出し時に EnemyEntityPresenter から渡される
        Func<Vector3> getEnemyPosition;

        public void SetEnemyPositionProvider(Func<Vector3> provider)
        {
            getEnemyPosition = provider;
        }

        public IAttackStrategy CreateStrategy(IPlayerPositionProvider playerPositionProvider)
        {
            return new AimAttackStrategy(attackInterval, playerPositionProvider, getEnemyPosition ?? (() => Vector3.zero));
        }
    }
}
