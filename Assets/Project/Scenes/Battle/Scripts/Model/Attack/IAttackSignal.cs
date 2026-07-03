using System;
using Project.Scripts.Extensions;

namespace Project.Scenes.Battle.Scripts.Model.Attack
{
    public interface IAttackSignal
    {
        AttackEvent CreateEvent(IDirectionProvider directionProvider, IRotationProvider rotationProvider, int sourceIndex = 0, SeType seType = SeType.None);
        IAttackSignal Clone();
    }
}
