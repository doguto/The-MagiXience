using System;
using UniRx;
using UnityEngine;
using Project.Scenes.Battle.Scripts.Model.Movement;
using Project.Scenes.Battle.Scripts.Model.Attack;

namespace Project.Scenes.Battle.Scripts.Model.Entity
{
    public abstract class EntityBase
    {
        readonly ReactiveProperty<int> currentHp = new();
        readonly CompositeDisposable disposables = new();
        readonly Subject<Unit> onDeath = new();

        public IObservable<Unit> OnDeath => onDeath;

        IMovementStrategy movementStrategy;
        IAttackStrategy attackStrategy;

        protected EntityBase(int maxHp, Vector3 initialPosition)
        {
            MaxHp = maxHp;
            currentHp.Value = maxHp;
            Position = initialPosition;
            currentHp.AddTo(disposables);
        }

        public int MaxHp { get; }
        public IReadOnlyReactiveProperty<int> CurrentHp => currentHp;
        public bool IsAlive => currentHp.Value > 0;
        public Vector3 Position { get; protected set; }

        public void TakeDamage(int damage)
        {
            if (!IsAlive) return;
            if (damage < 0)
            {
                Debug.LogWarning($"Negative damage value: {damage}");
                return;
            }

            currentHp.Value = Mathf.Max(0, currentHp.Value - damage);

            if (currentHp.Value <= 0)
            {
                onDeath.OnNext(Unit.Default);
                OnDeathCore();
            }
        }

        public void SetMovementStrategy(IMovementStrategy strategy)
        {
            movementStrategy = strategy;
            movementStrategy?.Initialize();
        }

        public void UpdateMovement(float deltaTime)
        {
            if (movementStrategy == null || !IsAlive) return;

            Vector3 newPosition = movementStrategy.UpdateMovement(Position, deltaTime);
            Position = newPosition;
        }

        public IMovementStrategy MovementStrategy => movementStrategy;

        public void SetAttackStrategy(IAttackStrategy strategy)
        {
            attackStrategy = strategy;
            attackStrategy?.Initialize();
        }

        public void UpdateAttack(float deltaTime)
        {
            if (attackStrategy == null || !IsAlive) return;

            attackStrategy.Update(deltaTime);
        }

        public IAttackStrategy AttackStrategy => attackStrategy;
        public bool CanAttack => attackStrategy?.CanAttack ?? false;

        // ObjectPool用
        protected void ResetHp()
        {
            currentHp.Value = MaxHp;
        }

        public abstract void OnCollision(EntityBase other);

        public abstract bool IsPlayer { get; }

        protected virtual void OnDeathCore() { }

        public virtual void Dispose()
        {
            disposables.Dispose();
        }
    }
}
