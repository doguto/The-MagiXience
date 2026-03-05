using System;
using UniRx;

namespace Project.Scenes.Battle.Scripts.Model
{
    public class AllEnemiesDefeatedBattlePhaseModel : BattlePhaseModelBase
    {
        readonly IEnemyTracker tracker;
        bool hasSpawned;

        public AllEnemiesDefeatedBattlePhaseModel(BattlePhaseDefinition definition, IEnemyTracker tracker)
            : base(definition)
        {
            this.tracker = tracker;
        }

        protected override void OnEnter()
        {
            hasSpawned = false;

            tracker.OnEnemySpawned
                .Subscribe(_ => hasSpawned = true)
                .AddTo(Disposables);

            tracker.OnEnemyRemoved
                .Subscribe(_ => CheckAllDefeated())
                .AddTo(Disposables);
        }

        void CheckAllDefeated()
        {
            if (hasSpawned && tracker.ActiveEnemyCount == 0)
            {
                CompletePhase();
            }
        }
    }
}
