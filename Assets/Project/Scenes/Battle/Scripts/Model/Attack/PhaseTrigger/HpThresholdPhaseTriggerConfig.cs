using System;
using UniRx;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Model.Attack.PhaseTrigger
{
    [Serializable]
    public class HpThresholdPhaseTriggerConfig : IAttackPhaseTriggerConfig
    {
        [SerializeField] int hpThreshold;

        public IObservable<Unit> CreateTrigger(AttackPhaseTriggerContext context)
        {
            if (context.CurrentHp == null) return Observable.Never<Unit>();

            return context.CurrentHp
                .Where(hp => hp <= hpThreshold)
                .Select(_ => Unit.Default)
                .Take(1);
        }

        public IAttackPhaseTriggerConfig Clone() => new HpThresholdPhaseTriggerConfig { hpThreshold = hpThreshold };
    }
}
