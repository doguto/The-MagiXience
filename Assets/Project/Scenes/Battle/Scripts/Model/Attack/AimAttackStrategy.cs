using System;
using UniRx;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Model.Attack
{
    public class AimAttackStrategy : IAttackStrategy
    {
        readonly float attackInterval;
        readonly IPlayerPositionProvider playerPositionProvider;
        readonly Func<Vector3> getEnemyPosition;
        readonly Subject<AttackEvent> onAttackTiming = new();
        readonly CompositeDisposable disposables = new();

        public IObservable<AttackEvent> OnAttackTiming => onAttackTiming;

        public AimAttackStrategy(float attackInterval, IPlayerPositionProvider playerPositionProvider, Func<Vector3> getEnemyPosition)
        {
            this.attackInterval = attackInterval;
            this.playerPositionProvider = playerPositionProvider;
            this.getEnemyPosition = getEnemyPosition;
        }

        public void Initialize()
        {
            Observable.Interval(TimeSpan.FromSeconds(attackInterval))
                .Subscribe(_ => onAttackTiming.OnNext(CreateAttackEvent()))
                .AddTo(disposables);
        }

        public void Update(float deltaTime) { }

        AttackEvent CreateAttackEvent()
        {
            Vector3 enemyPos = getEnemyPosition();
            Vector3 playerPos = playerPositionProvider.PlayerPosition;
            Vector2 direction = ((Vector2)(playerPos - enemyPos)).normalized;
            if (direction == Vector2.zero) direction = Vector2.left;
            return AttackEvent.Single(direction);
        }

        public void Dispose()
        {
            disposables?.Dispose();
            onAttackTiming?.Dispose();
        }
    }
}
