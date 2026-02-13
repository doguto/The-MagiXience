using UnityEngine;
using Project.Scenes.Battle.Scripts.Model.Movement;

namespace Project.Scenes.Battle.Scripts.Model.Entity
{
    public class BulletEntityModel : EntityBase
    {
        public BulletEntityModel(int damage, Vector3 initialPosition, IMovementStrategy movementStrategy, bool isFriendly) 
            : base(1, initialPosition)
        {
            Damage = damage;
            IsFriendly = isFriendly;
            SetMovementStrategy(movementStrategy);
        }

        public override bool IsPlayer => IsFriendly;
        
        public int Damage { get; private set; }
        
        public bool IsFriendly { get; private set; }


        public override void OnCollision(EntityBase other)
        {
            if (other == null || !other.IsAlive) return;
            
            // 味方の弾は敵に、敵の弾はプレイヤーに当たる
            bool shouldHit = IsFriendly ? !other.IsPlayer : other.IsPlayer;
            
            if (shouldHit)
            {
                // 弾自体は衝突したら消える（HP=0にする）
                TakeDamage(1);
            }
        }
        
        public void Reinitialize(int damage, Vector3 position, IMovementStrategy movementStrategy, bool isFriendly)
        {
            Damage = damage;
            Position = position;
            IsFriendly = isFriendly;
            ResetHp();
            SetMovementStrategy(movementStrategy);
        }

        protected override void OnDeath()
        {
            Debug.Log($"[BulletEntityModel] Bullet destroyed at position {Position}");
        }
    }
}
