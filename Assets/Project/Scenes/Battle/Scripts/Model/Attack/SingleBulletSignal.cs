using System;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Model.Attack
{
    [Serializable]
    public class SingleBulletSignal : IAttackSignal
    {
        public IAttackSignal Clone() => new SingleBulletSignal();

        public AttackEvent CreateEvent(IDirectionProvider directionProvider, int bulletPoolIndex = 0)
        {
            return AttackEvent.Single(directionProvider.GetDirection(), bulletPoolIndex);
        }
    }
}
