using System;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Model.Attack
{
    [Serializable]
    public class SingleBulletSignal : IAttackSignal
    {
        public IAttackSignal Clone() => new SingleBulletSignal();

        public AttackEvent CreateEvent(IDirectionProvider directionProvider)
        {
            return AttackEvent.Single(directionProvider.GetDirection());
        }
    }
}
