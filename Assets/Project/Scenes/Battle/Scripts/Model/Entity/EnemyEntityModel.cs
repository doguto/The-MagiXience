using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Model.Entity
{
    public class EnemyEntityModel : EntityBase
    {
        public EnemyEntityModel(int maxHp, Vector3 initialPosition, int contactDamage) 
            : base(maxHp, initialPosition)
        {
            ContactDamage = contactDamage;
        }

        public override bool IsPlayer => false;
        
        public int ContactDamage { get; }
        
        public override void OnCollision(EntityBase other)
        {
            if (other == null || !other.IsAlive) return;

            // Layerフィルタ済みなのでPlayerBulletのみ到達する
            if (other is BulletEntityModel bullet)
            {
                TakeDamage(bullet.Damage);
            }
        }

        protected override void OnDeathCore()
        {
            Debug.Log($"[EnemyEntityModel] Enemy died at position {Position}");
        }
    }
}
