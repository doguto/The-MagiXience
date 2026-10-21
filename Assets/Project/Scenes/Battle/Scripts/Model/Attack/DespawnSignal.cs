using System;
using Project.Scripts.Extensions;

namespace Project.Scenes.Battle.Scripts.Model.Attack
{
    /// <summary>発射元の敵を撃破扱いにせず退場させる。死亡時攻撃は発火しない。</summary>
    [Serializable]
    public class DespawnSignal : IAttackSignal
    {
        public IAttackSignal Clone() => new DespawnSignal();

        public AttackEvent CreateEvent(IDirectionProvider directionProvider, IRotationProvider rotationProvider, int sourceIndex = 0, SeType seType = SeType.None)
        {
            return AttackEvent.Despawn();
        }
    }
}
