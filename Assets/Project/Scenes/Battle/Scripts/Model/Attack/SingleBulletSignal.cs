using System;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Model.Attack
{
    [Serializable]
    public class SingleBulletSignal : IAttackSignal
    {
        public AttackEvent CreateEvent(IDirectionProvider directionProvider)
        {
            return AttackEvent.Single(directionProvider.GetDirection());
        }
    }
}
