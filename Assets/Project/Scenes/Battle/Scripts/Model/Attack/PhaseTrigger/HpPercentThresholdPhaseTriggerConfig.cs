using System;
using UniRx;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Model.Attack.PhaseTrigger
{
    [Serializable]
    public class HpPercentThresholdPhaseTriggerConfig : IAttackPhaseTriggerConfig
    {
        [SerializeField, Range(0f, 100f)] float hpThresholdPercent = 50f;

        public IObservable<Unit> CreateTrigger(AttackPhaseTriggerContext context)
        {
            if (context.CurrentHp == null) return Observable.Never<Unit>();

            var threshold = context.MaxHp * hpThresholdPercent / 100f;
            return context.CurrentHp
                .Where(hp => hp <= threshold)
                .Select(_ => Unit.Default)
                .Take(1);
        }

        public IAttackPhaseTriggerConfig Clone() => new HpPercentThresholdPhaseTriggerConfig { hpThresholdPercent = hpThresholdPercent };
    }
}
