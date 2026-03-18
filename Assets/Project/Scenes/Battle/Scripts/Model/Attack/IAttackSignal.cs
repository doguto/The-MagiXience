using System;

namespace Project.Scenes.Battle.Scripts.Model.Attack
{
    public interface IAttackSignal
    {
        AttackEvent CreateEvent(IDirectionProvider directionProvider, int bulletPoolIndex = 0);
        IAttackSignal Clone();
    }
}
