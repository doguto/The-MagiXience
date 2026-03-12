using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Model.Entity
{
    public class BulletEntityModel : EntityBase
    {
        public BulletEntityModel(int damage)
            : base(1)
        {
            Damage = damage;
        }

        public override bool IsPlayer => false;

        public int Damage { get; private set; }

        public override void OnCollision(EntityBase other)
        {
            TakeDamage(1);
        }

        public void Reinitialize(int damage)
        {
            Damage = damage;
            ResetHp();
        }

        protected override void OnDeathCore()
        {
            Debug.Log($"[BulletEntityModel] Bullet destroyed.");
        }
    }
}
