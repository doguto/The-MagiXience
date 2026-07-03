using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Model.Entity
{
    public class EnemyEntityModel : EntityBase
    {
        public EnemyEntityModel(int maxHp, int contactDamage)
            : base(maxHp)
        {
            ContactDamage = contactDamage;
        }

        public override bool IsPlayer => false;

        public int ContactDamage { get; }

        public override void OnCollision(EntityBase other)
        {
            if (other is BulletEntityModel bullet)
            {
                TakeDamage(bullet.Damage);
            }
        }

        protected override void OnDeathCore()
        {
            Debug.Log("[EnemyEntityModel] Enemy died.");
        }
    }
}
