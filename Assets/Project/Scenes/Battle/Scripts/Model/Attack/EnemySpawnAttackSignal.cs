using System;
using Project.Scripts.Extensions;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Model.Attack
{
    [Serializable]
    public class EnemySpawnAttackSignal : IAttackSignal
    {
        [SerializeField] Vector2 offset;

        public IAttackSignal Clone() => new EnemySpawnAttackSignal { offset = offset };

        public AttackEvent CreateEvent(IDirectionProvider directionProvider, IRotationProvider rotationProvider, int sourceIndex = 0, SeType seType = SeType.None)
        {
            return AttackEvent.Spawn(directionProvider.GetDirection(), rotationProvider.GetRotation(), sourceIndex, offset, seType);
        }
    }
}
