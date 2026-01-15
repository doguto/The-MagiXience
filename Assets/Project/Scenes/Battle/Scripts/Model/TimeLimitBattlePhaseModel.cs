using System;
using UniRx;

namespace Project.Scenes.Battle.Scripts.Model
{
    public class TimeLimitBattlePhaseModel : BattlePhaseModelBase
    {
        public TimeLimitBattlePhaseModel(BattlePhaseDefinition definition) : base(definition)
        {
            
        }

        protected override void OnEnter()
        {
            Observable.Timer(TimeSpan.FromSeconds(TimeLimitSeconds))
                .Subscribe(_ => CompletePhase())
                .AddTo(Disposables);
        }
    }
}
