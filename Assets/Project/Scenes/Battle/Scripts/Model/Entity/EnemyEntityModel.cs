using System;
using UniRx;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Model.Entity
{
    public class EnemyEntityModel : EntityBase
    {
        readonly Subject<Unit> onIneffectiveHit = new();

        public EnemyEntityModel(int maxHp, int contactDamage, bool onlyChargeDamageable = false)
            : base(maxHp)
        {
            ContactDamage = contactDamage;
            OnlyChargeDamageable = onlyChargeDamageable;
        }

        public override bool IsPlayer => false;

        public int ContactDamage { get; }

        // 通常攻撃では被ダメージ0、チャージ攻撃のみ被ダメージする敵か
        public bool OnlyChargeDamageable { get; }

        // 通常攻撃無効の敵に通常弾が当たり、ダメージが無効化された時に発火
        public IObservable<Unit> OnIneffectiveHit => onIneffectiveHit;

        public bool IsInvincible { get; private set; }

        public void SetInvincible(bool enabled) => IsInvincible = enabled;

        public override void TakeDamage(int damage)
        {
            if (IsInvincible) return;
            base.TakeDamage(damage);
        }

        public override void OnCollision(EntityBase other)
        {
            if (other is BulletEntityModel bullet)
            {
                if (IsInvincible)
                {
                    onIneffectiveHit.OnNext(Unit.Default);
                    return;
                }

                // 通常攻撃無効の敵に通常弾が当たった場合は被ダメージ0（＝ダメージ処理をスキップ）
                if (OnlyChargeDamageable && !bullet.IsPlayerChargeBullet)
                {
                    onIneffectiveHit.OnNext(Unit.Default);
                    return;
                }

                TakeDamage(bullet.Damage);
            }
        }

        protected override void OnDeathCore()
        {
            Debug.Log("[EnemyEntityModel] Enemy died.");
        }

        public override void Dispose()
        {
            onIneffectiveHit.Dispose();
            base.Dispose();
        }
    }
}
