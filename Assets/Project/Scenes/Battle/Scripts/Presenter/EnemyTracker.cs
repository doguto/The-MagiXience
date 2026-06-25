using System;
using UniRx;
using UniRx.Triggers;
using UnityEngine;
using Project.Scenes.Battle.Scripts.Model;
using Project.Scenes.Battle.Scripts.Presenter.Entity;

namespace Project.Scenes.Battle.Scripts.Presenter
{
    public class EnemyTracker : MonoBehaviour, IEnemyTracker
    {
        readonly Subject<Unit> enemySpawned = new();
        readonly Subject<Unit> enemyRemoved = new();
        readonly CompositeDisposable disposables = new();
        int activeCount;

        public IObservable<Unit> OnEnemySpawned => enemySpawned;
        public IObservable<Unit> OnEnemyRemoved => enemyRemoved;
        public int ActiveEnemyCount => activeCount;

        public void RegisterEnemy(EnemyEntityPresenter enemy)
        {
            activeCount++;
            enemySpawned.OnNext(Unit.Default);

            enemy.gameObject.OnDestroyAsObservable()
                .Take(1)
                .Subscribe(_ =>
                {
                    activeCount--;
                    enemyRemoved.OnNext(Unit.Default);
                })
                .AddTo(disposables);
        }

        void OnDestroy()
        {
            disposables.Dispose();
            enemySpawned.Dispose();
            enemyRemoved.Dispose();
        }
    }
}
