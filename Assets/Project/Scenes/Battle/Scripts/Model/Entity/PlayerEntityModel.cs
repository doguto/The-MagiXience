using System;
using UniRx;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Model.Entity
{
    public class PlayerEntityModel : EntityBase
    {
        readonly ReactiveProperty<bool> isSneaking = new(false);
        readonly ReactiveProperty<float> chargeTime = new(0f);
        readonly ReactiveProperty<bool> isInvincible = new(false);

        float invincibilityTimer;

        public PlayerEntityModel(int maxHp, Vector3 initialPosition, float chargeThreshold = 1.0f, float sneakSpeedMultiplier = 0.5f, float invincibilityDuration = 1.0f)
            : base(maxHp, initialPosition)
        {
            ChargeThreshold = chargeThreshold;
            SneakSpeedMultiplier = sneakSpeedMultiplier;
            InvincibilityDuration = invincibilityDuration;
        }

        public override bool IsPlayer => true;

        public float ChargeThreshold { get; }
        public float SneakSpeedMultiplier { get; }
        public float InvincibilityDuration { get; }
        public IReadOnlyReactiveProperty<bool> IsSneaking => isSneaking;
        public IReadOnlyReactiveProperty<float> ChargeTime => chargeTime;
        public bool IsChargeComplete => chargeTime.Value >= ChargeThreshold;
        public IReadOnlyReactiveProperty<bool> IsInvincible => isInvincible;

        public void SetSneaking(bool sneaking)
        {
            isSneaking.Value = sneaking;

            if (!sneaking)
            {
                chargeTime.Value = 0f;
            }
        }

        public void UpdateCharge(float deltaTime)
        {
            if (!isSneaking.Value) return;

            chargeTime.Value = Mathf.Min(chargeTime.Value + deltaTime, ChargeThreshold);
        }

        public void UpdateInvincibility(float deltaTime)
        {
            if (!isInvincible.Value) return;

            invincibilityTimer -= deltaTime;
            if (invincibilityTimer <= 0f)
            {
                isInvincible.Value = false;
            }
        }

        void StartInvincibility()
        {
            isInvincible.Value = true;
            invincibilityTimer = InvincibilityDuration;
        }

        public void ResetCharge()
        {
            chargeTime.Value = 0f;
        }

        public void TakeDamageWithInvincibility(int damage)
        {
            if (isInvincible.Value) return;

            TakeDamage(damage);

            if (IsAlive)
            {
                StartInvincibility();
            }
        }

        public override void OnCollision(EntityBase other)
        {
            if (other == null || !other.IsAlive) return;
            if (other.IsPlayer) return;

            if (other is EnemyEntityModel enemy)
            {
                TakeDamageWithInvincibility(enemy.ContactDamage);
            }
            else if (other is BulletEntityModel bullet && !bullet.IsFriendly)
            {
                TakeDamageWithInvincibility(bullet.Damage);
            }
        }

        protected override void OnDeathCore()
        {
            Debug.Log("[PlayerEntityModel] Player died");
        }

        public override void Dispose()
        {
            base.Dispose();
            isSneaking?.Dispose();
            chargeTime?.Dispose();
            isInvincible?.Dispose();
        }
    }
}
