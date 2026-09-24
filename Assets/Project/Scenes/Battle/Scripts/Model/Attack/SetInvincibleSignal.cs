using System;
using Project.Scripts.Extensions;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Model.Attack
{
    /// <summary>発射元の敵の無敵状態を切り替える。弾は出さない。</summary>
    [Serializable]
    public class SetInvincibleSignal : IAttackSignal
    {
        [SerializeField] bool enabled = true;

        public IAttackSignal Clone() => new SetInvincibleSignal { enabled = enabled };

        public AttackEvent CreateEvent(IDirectionProvider directionProvider, IRotationProvider rotationProvider, int sourceIndex = 0, SeType seType = SeType.None)
        {
            return AttackEvent.SetInvincible(enabled);
        }
    }
}
