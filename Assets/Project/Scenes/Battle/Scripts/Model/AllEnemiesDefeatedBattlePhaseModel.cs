using UniRx;

namespace Project.Scenes.Battle.Scripts.Model
{
    public class AllEnemiesDefeatedBattlePhaseModel : BattlePhaseModelBase
    {
        readonly IEnemyTracker tracker;
        readonly int totalSpawnCount;
        int spawnedCount;
        bool allSpawned;

        public AllEnemiesDefeatedBattlePhaseModel(BattlePhaseDefinition definition, IEnemyTracker tracker)
            : base(definition)
        {
            this.tracker = tracker;
            totalSpawnCount = definition.TimelineBuilder != null
                ? definition.TimelineBuilder.TotalEnemySpawnCount
                : 0;
        }

        protected override void OnEnter()
        {
            spawnedCount = 0;
            allSpawned = totalSpawnCount <= 0;

            tracker.OnEnemySpawned
                .Subscribe(_ =>
                {
                    spawnedCount++;
                    if (spawnedCount >= totalSpawnCount)
                    {
                        allSpawned = true;
                    }
                })
                .AddTo(Disposables);

            tracker.OnEnemyRemoved
                .Subscribe(_ => CheckAllDefeated())
                .AddTo(Disposables);
        }

        void CheckAllDefeated()
        {
            if (allSpawned && tracker.ActiveEnemyCount == 0)
            {
                CompletePhase();
            }
        }
    }
}
