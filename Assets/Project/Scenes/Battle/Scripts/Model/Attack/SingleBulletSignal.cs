using System;
using Project.Scripts.Extensions;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Model.Attack
{
    [Serializable]
    public class SingleBulletSignal : IAttackSignal
    {
        public IAttackSignal Clone() => new SingleBulletSignal();

        public AttackEvent CreateEvent(IDirectionProvider directionProvider, int bulletPoolIndex = 0, SeType seType = SeType.None)
        {
            return AttackEvent.Single(directionProvider.GetDirection(), bulletPoolIndex, seType);
        }
    }
}
