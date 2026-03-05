using System;
using UniRx;

namespace Project.Scenes.Battle.Scripts.Model
{
    public interface IEnemyTracker
    {
        IObservable<Unit> OnEnemySpawned { get; }
        IObservable<Unit> OnEnemyRemoved { get; }
        int ActiveEnemyCount { get; }
    }
}
