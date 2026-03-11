using System;
using System.Collections.Generic;
using UniRx;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Model.Attack
{
    public class NWayAttackStrategy : IAttackStrategy
    {
        readonly float attackInterval;
        readonly int wayCount;
        readonly float spreadAngle;
        readonly IDirectionProvider directionProvider;
        readonly Subject<AttackEvent> onAttackTiming = new();
        readonly CompositeDisposable disposables = new();

        public IObservable<AttackEvent> OnAttackTiming => onAttackTiming;

        public NWayAttackStrategy(float attackInterval, int wayCount, float spreadAngle, IDirectionProvider directionProvider)
        {
            this.attackInterval = attackInterval;
            this.wayCount = wayCount;
            this.spreadAngle = spreadAngle;
            this.directionProvider = directionProvider;
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
            var baseDirection = directionProvider.GetDirection();
            var directions = new List<Vector2>(wayCount);
            float baseAngle = Mathf.Atan2(baseDirection.y, baseDirection.x) * Mathf.Rad2Deg;

            if (wayCount == 1)
            {
                directions.Add(baseDirection);
            }
            else
            {
                float halfSpread = spreadAngle / 2f;
                float step = spreadAngle / (wayCount - 1);
                for (int i = 0; i < wayCount; i++)
                {
                    float angle = baseAngle - halfSpread + step * i;
                    float rad = angle * Mathf.Deg2Rad;
                    directions.Add(new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)));
                }
            }

            return new AttackEvent(directions);
        }

        public void Dispose()
        {
            disposables?.Dispose();
            onAttackTiming?.Dispose();
        }
    }
}
