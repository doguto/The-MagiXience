using System;
using UniRx;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Model
{
    public class AllEnemiesDefeatedBattlePhaseModel : BattlePhaseModelBase
    {
        readonly IEnemyTracker tracker;
        readonly double lastSpawnTime;
        bool allSpawned;

        public AllEnemiesDefeatedBattlePhaseModel(BattlePhaseDefinition definition, IEnemyTracker tracker)
            : base(definition)
        {
            this.tracker = tracker;
            lastSpawnTime = definition.TimelineBuilder != null
                ? definition.TimelineBuilder.LastEnemySpawnTime
                : -1;
        }

        protected override void OnEnter()
        {
            allSpawned = lastSpawnTime < 0;

            tracker.OnEnemyRemoved
                .Subscribe(_ => CheckAllDefeated())
                .AddTo(Disposables);
            
            if (Director != null)
            {
                Observable.Timer(TimeSpan.FromSeconds(lastSpawnTime))
                    .Where(_ => !allSpawned && Director != null)
                    .Take(1)
                    .Subscribe(_ =>
                    {
                        allSpawned = true;
                        CheckAllDefeated();
                    })
                    .AddTo(Disposables);
            }
            else
            {
                allSpawned = true;
            }
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
