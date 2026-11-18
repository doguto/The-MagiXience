using System;
using Project.Scenes.Battle.Scripts.Model.Movement;
using Project.Scripts.Extensions;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Model.Attack
{
    /// <summary>発射元の敵の移動を中断し、指定した移動に差し替える。弾は出さない。</summary>
    [Serializable]
    public class ChangeMovementSignal : IAttackSignal
    {
        [SerializeReference, SubclassSelector] IMovementStep movement;

        public IAttackSignal Clone() => new ChangeMovementSignal { movement = movement };

        public AttackEvent CreateEvent(IDirectionProvider directionProvider, IRotationProvider rotationProvider, int sourceIndex = 0, SeType seType = SeType.None)
        {
            return AttackEvent.ChangeMovement(movement);
        }
    }
}
