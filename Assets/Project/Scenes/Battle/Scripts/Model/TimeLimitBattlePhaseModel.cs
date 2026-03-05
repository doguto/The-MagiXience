using System;
using UniRx;

namespace Project.Scenes.Battle.Scripts.Model
{
    public class TimeLimitBattlePhaseModel : BattlePhaseModelBase
    {
        readonly float timeLimitSeconds;

        public TimeLimitBattlePhaseModel(BattlePhaseDefinition definition, float timeLimitSeconds) : base(definition)
        {
            this.timeLimitSeconds = timeLimitSeconds;
        }

        protected override void OnEnter()
        {
            Observable.Timer(TimeSpan.FromSeconds(timeLimitSeconds))
                .Subscribe(_ => CompletePhase())
                .AddTo(Disposables);
        }
    }
}
