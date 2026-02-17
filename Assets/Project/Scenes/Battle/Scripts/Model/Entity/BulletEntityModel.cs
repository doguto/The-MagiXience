using UnityEngine;
using Project.Scenes.Battle.Scripts.Model.Movement;

namespace Project.Scenes.Battle.Scripts.Model.Entity
{
    public class BulletEntityModel : EntityBase
    {
        public BulletEntityModel(int damage, Vector3 initialPosition, IMovementStrategy movementStrategy)
            : base(1, initialPosition)
        {
            Damage = damage;
            SetMovementStrategy(movementStrategy);
        }

        public override bool IsPlayer => false;

        public int Damage { get; private set; }


        public override void OnCollision(EntityBase other)
        {
            if (other == null || !other.IsAlive) return;
            // Layerフィルタ済みなので当たったら消滅
            TakeDamage(1);
        }
        
        public void Reinitialize(int damage, Vector3 position, IMovementStrategy movementStrategy)
        {
            Damage = damage;
            Position = position;
            ResetHp();
            SetMovementStrategy(movementStrategy);
        }

        protected override void OnDeathCore()
        {
            Debug.Log($"[BulletEntityModel] Bullet destroyed at position {Position}");
        }
    }
}
