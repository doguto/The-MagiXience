using System;
using UniRx;
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

            // プールで使い回す敵は破棄されないので、「場から消えた」通知で数える。
            // 敵1体ごとに購読が積み上がらないよう、通知を受けたら自身をdisposablesから外す
            IDisposable subscription = null;
            subscription = enemy.OnRemoved
                .Take(1)
                .Subscribe(_ =>
                {
                    activeCount--;
                    enemyRemoved.OnNext(Unit.Default);
                    if (subscription != null) disposables.Remove(subscription);
                });
            disposables.Add(subscription);
        }

        void OnDestroy()
        {
            disposables.Dispose();
            enemySpawned.Dispose();
            enemyRemoved.Dispose();
        }
    }
}
