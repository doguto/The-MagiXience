using System;
using UniRx;
using UnityEngine;
using Project.Scenes.Battle.Scripts.Model.Attack;

namespace Project.Scenes.Battle.Scripts.Model.Entity
{
    public abstract class EntityBase
    {
        readonly ReactiveProperty<int> currentHp = new();
        readonly CompositeDisposable disposables = new();
        readonly Subject<Unit> onDeath = new();

        public IObservable<Unit> OnDeath => onDeath;

        IAttackStrategy attackStrategy;

        protected EntityBase(int maxHp)
        {
            MaxHp = maxHp;
            currentHp.Value = maxHp;
            currentHp.AddTo(disposables);
        }

        public int MaxHp { get; private set; }
        public IReadOnlyReactiveProperty<int> CurrentHp => currentHp;
        public bool IsAlive => currentHp.Value > 0;

        // 最大HPを再設定する。現在HPが新しい最大値を超える場合は切り詰める。
        protected void SetMaxHp(int value)
        {
            MaxHp = Mathf.Max(1, value);
            if (currentHp.Value > MaxHp)
            {
                currentHp.Value = MaxHp;
            }
        }

        public virtual void TakeDamage(int damage)
        {
            if (!IsAlive) return;
            if (damage < 0)
            {
                Debug.LogWarning($"Negative damage value: {damage}");
                return;
            }

            SetCurrentHp(currentHp.Value - damage);
        }

        protected void SetCurrentHp(int value)
        {
            currentHp.Value = Mathf.Max(0, value);

            if (currentHp.Value <= 0)
            {
                onDeath.OnNext(Unit.Default);
                OnDeathCore();
            }
        }

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
