using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.SceneManagement;
using Project.Scenes.Battle.Scripts.Model.Movement;

namespace Project.Scenes.Battle.Scripts.Presenter.Entity
{
    public class BulletPool : MonoBehaviour
    {
        [SerializeField] BulletEntityPresenter bulletPrefab;
        [SerializeField] int defaultCapacity = 20;
        [SerializeField] int maxSize = 100;

        // 敵ごとにプールを持つと、短命な敵は毎回Instantiateから始まり、死亡時に弾が破棄されてしまう。
        // 同じ弾Prefabを使う全BulletPoolで1つのプールを共有し、敵の生成・破棄をまたいで弾を再利用する。
        static readonly Dictionary<BulletEntityPresenter, IObjectPool<BulletEntityPresenter>> sharedPools = new();
        static bool isSceneHookRegistered;

        IObjectPool<BulletEntityPresenter> pool;

        void Awake()
        {
            pool = GetOrCreateSharedPool(bulletPrefab, defaultCapacity, maxSize);
        }

        /// <param name="range">飛距離の上限(ワールド単位)。0以下で無制限。ビームの終点表現に使う</param>
        /// <param name="startDelay">生成後、移動を開始するまでの待機秒数。0で従来通り即座に移動開始</param>
        /// <param name="movementOverride">この弾に限り使う移動ステップ。nullならPrefab側のmovementStepsを使う(従来動作)</param>
        public BulletEntityPresenter SpawnBullet(int damage, Vector3 position, Vector2 direction = default, bool isPlayerBullet = false, Quaternion rotation = default, float range = 0f, float startDelay = 0f, IMovementStep movementOverride = null)
        {
            var bullet = pool.Get();
            bullet.Initialize(damage, position, direction, pool, isPlayerBullet, rotation, range, startDelay, movementOverride);
            return bullet;
        }

        static IObjectPool<BulletEntityPresenter> GetOrCreateSharedPool(BulletEntityPresenter prefab, int defaultCapacity, int maxSize)
        {
            if (!isSceneHookRegistered)
            {
                // 弾はシーンのルートに生成されるため、シーン破棄で待機中の弾も破棄される。
                // 破棄済みの弾を取り出さないよう、シーン破棄のたびに待機中の弾を捨てる。
                SceneManager.sceneUnloaded += _ => ClearSharedPools();
                isSceneHookRegistered = true;
            }

            if (prefab == null) return CreatePool(prefab, defaultCapacity, maxSize);

            if (!sharedPools.TryGetValue(prefab, out var shared))
            {
                shared = CreatePool(prefab, defaultCapacity, maxSize);
                sharedPools.Add(prefab, shared);
            }

            return shared;
        }

        static IObjectPool<BulletEntityPresenter> CreatePool(BulletEntityPresenter prefab, int defaultCapacity, int maxSize)
        {
            return new ObjectPool<BulletEntityPresenter>(
                // 親をBulletPoolにすると、Poolが他Entityの子の場合に
                // 親のRigidbody2DにOnTriggerEnter2Dが伝播してしまうため、ルートに生成する
                createFunc: () => Instantiate(prefab),
                actionOnGet: bullet => bullet.OnTakenFromPool(),
                actionOnRelease: bullet => bullet.OnReturnedToPool(),
                actionOnDestroy: bullet =>
                {
                    if (bullet == null) return;
                    Destroy(bullet.gameObject);
                },
                // 二重返却はBulletEntityPresenter側で防いでいるため、返却毎の線形探索はEditorでのみ行う
                collectionCheck: Application.isEditor,
                defaultCapacity: defaultCapacity,
                maxSize: maxSize
            );
        }

        static void ClearSharedPools()
        {
            foreach (var shared in sharedPools.Values)
            {
                shared.Clear();
            }
            sharedPools.Clear();
        }
    }
}
