using System;
using UniRx;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Model.Attack.PhaseTrigger
{
    [Serializable]
    public class TimeElapsedPhaseTriggerConfig : IAttackPhaseTriggerConfig
    {
        [SerializeField, Min(0.1f)] float delaySeconds = 5f;

        public IObservable<Unit> CreateTrigger(AttackPhaseTriggerContext context)
        {
            return Observable.Timer(TimeSpan.FromSeconds(delaySeconds)).Select(_ => Unit.Default);
        }

        public IAttackPhaseTriggerConfig Clone() => new TimeElapsedPhaseTriggerConfig { delaySeconds = delaySeconds };
    }
}
