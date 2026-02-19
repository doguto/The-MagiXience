using UnityEngine;
using UnityEngine.Pool;
using Project.Scenes.Battle.Scripts.Model.Movement;

namespace Project.Scenes.Battle.Scripts.Presenter.Entity
{
    public class BulletPool : MonoBehaviour
    {
        [SerializeField] BulletEntityPresenter bulletPrefab;
        [SerializeField] int defaultCapacity = 20;
        [SerializeField] int maxSize = 100;

        IObjectPool<BulletEntityPresenter> pool;

        void Awake()
        {
            pool = new ObjectPool<BulletEntityPresenter>(
                createFunc: CreateBullet,
                actionOnGet: OnGetBullet,
                actionOnRelease: OnReleaseBullet,
                actionOnDestroy: OnDestroyBullet,
                collectionCheck: true,
                defaultCapacity: defaultCapacity,
                maxSize: maxSize
            );
        }

        public BulletEntityPresenter SpawnBullet(int damage, Vector3 position, Vector3 velocity, bool isFriendly)
        {
            var bullet = pool.Get();
            bullet.Initialize(damage, position, new LinearMovement(velocity), isFriendly, pool);
            return bullet;
        }

        BulletEntityPresenter CreateBullet()
        {
            var bullet = Instantiate(bulletPrefab, transform);
            return bullet;
        }

        void OnGetBullet(BulletEntityPresenter bullet)
        {
            bullet.OnTakenFromPool();
        }

        void OnReleaseBullet(BulletEntityPresenter bullet)
        {
            bullet.OnReturnedToPool();
        }

        void OnDestroyBullet(BulletEntityPresenter bullet)
        {
            Destroy(bullet.gameObject);
        }

        void OnDestroy()
        {
            pool?.Clear();
        }
    }
}
