using System;
using UniRx;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Model.Attack
{
    public class IntervalAttackStrategy : IAttackStrategy
    {
        readonly float attackInterval;
        readonly IDirectionProvider directionProvider;
        readonly Subject<AttackEvent> onAttackTiming = new();
        readonly CompositeDisposable disposables = new();

        public IObservable<AttackEvent> OnAttackTiming => onAttackTiming;

        public IntervalAttackStrategy(float attackInterval, IDirectionProvider directionProvider)
        {
            this.attackInterval = attackInterval;
            this.directionProvider = directionProvider;
        }

        public void Initialize()
        {
            Observable.Interval(TimeSpan.FromSeconds(attackInterval))
                .Subscribe(_ => onAttackTiming.OnNext(AttackEvent.Single(directionProvider.GetDirection())))
                .AddTo(disposables);
        }

        public void Update(float deltaTime) { }

        public void Dispose()
        {
            disposables?.Dispose();
            onAttackTiming?.Dispose();
        }
    }
}
