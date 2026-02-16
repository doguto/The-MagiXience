using System;
using UniRx;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Model.Attack
{
    public class IntervalAttackStrategy : IAttackStrategy
    {
        readonly float attackInterval;
        readonly Subject<Unit> onAttackTiming = new();
        readonly CompositeDisposable disposables = new();

        public IObservable<Unit> OnAttackTiming => onAttackTiming;

        public IntervalAttackStrategy(float attackInterval)
        {
            this.attackInterval = attackInterval;
        }

        public void Initialize()
        {
            Observable.Interval(TimeSpan.FromSeconds(attackInterval))
                .Subscribe(_ => onAttackTiming.OnNext(Unit.Default))
                .AddTo(disposables);
        }

        public void Update(float deltaTime)
        {
            // シンプル攻撃なので Update 不要
        }

        public void Dispose()
        {
            disposables?.Dispose();
            onAttackTiming?.Dispose();
        }
    }
}
