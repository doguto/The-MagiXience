using UnityEngine;
using UnityEngine.Pool;

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

        public BulletEntityPresenter SpawnBullet(int damage, Vector3 position, Vector2 direction = default)
        {
            var bullet = pool.Get();
            bullet.Initialize(damage, position, direction, pool);
            return bullet;
        }

        BulletEntityPresenter CreateBullet()
        {
            // 親をBulletPoolにすると、Poolが他Entityの子の場合に
            // 親のRigidbody2DにOnTriggerEnter2Dが伝播してしまうため、ルートに生成する
            var bullet = Instantiate(bulletPrefab);
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
            if (bulletPrefab is null) return;
            pool.Clear();
        }
    }
}
