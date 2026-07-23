using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Model.Entity
{
    public class BulletEntityModel : EntityBase
    {
        public BulletEntityModel(int damage, bool isPlayerChargeBullet = false)
            : base(1)
        {
            Damage = damage;
            IsPlayerChargeBullet = isPlayerChargeBullet;
        }

        public override bool IsPlayer => false;

        bool IsPlayerChargeBullet { get; set; }

        public int Damage { get; private set; }

        public bool IsInvincible { get; private set; }

        public void SetInvincible(bool value) => IsInvincible = value;

        public override void OnCollision(EntityBase other)
        {
            if (IsInvincible)
                return;
            if (IsPlayerChargeBullet)
                return;
            TakeDamage(1);
        }

        public void Reinitialize(int damage, bool isPlayerBullet = false)
        {
            Damage = damage;
            IsPlayerChargeBullet = isPlayerBullet;
            IsInvincible = false;
            ResetHp();
        }

        protected override void OnDeathCore()
        {
        }
    }
}
