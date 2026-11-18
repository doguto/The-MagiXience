using System;
using UniRx;

namespace Project.Scenes.Battle.Scripts.Model.Attack.PhaseTrigger
{
    public interface IAttackPhaseTriggerConfig
    {
        IObservable<Unit> CreateTrigger(AttackPhaseTriggerContext context);
        IAttackPhaseTriggerConfig Clone();
    }
}
