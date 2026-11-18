using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.SceneManagement;

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
        // 弾はプールを作った時点のアクティブシーンのルートに生成される。そのシーンが閉じたときだけ捨てる
        // (会話シーンなど別シーンのアンロードで捨てると、事前生成した弾が失われ、飛行中の弾も古いプールへ戻ってしまう)
        static readonly Dictionary<BulletEntityPresenter, Scene> poolScenes = new();
        static bool isSceneHookRegistered;

        // 撃ち手が持つ弾の Prefab。実際の発射は BulletSystem が共有プールから行う
        public BulletEntityPresenter BulletPrefab => bulletPrefab;

        void Awake()
        {
            GetOrCreateSharedPool(bulletPrefab, defaultCapacity, maxSize);
        }

        public static IObjectPool<BulletEntityPresenter> GetOrCreateSharedPool(BulletEntityPresenter prefab, int defaultCapacity = 20, int maxSize = 100)
        {
            if (!isSceneHookRegistered)
            {
                SceneManager.sceneUnloaded += ClearPoolsOf;
                isSceneHookRegistered = true;
            }

            if (prefab == null) return CreatePool(prefab, defaultCapacity, maxSize);

            if (!sharedPools.TryGetValue(prefab, out var shared))
            {
                shared = CreatePool(prefab, defaultCapacity, maxSize);
                sharedPools.Add(prefab, shared);
                poolScenes[prefab] = SceneManager.GetActiveScene();
            }

            return shared;
        }

        // 最初の発射で生成が集中しないよう、指定数を先に作って待機させておく
        public static void Prewarm(BulletEntityPresenter prefab, int count)
        {
            if (prefab == null || count <= 0) return;
            var pool = GetOrCreateSharedPool(prefab);
            var missing = count - pool.CountInactive;
            if (missing <= 0) return;

            var taken = new List<BulletEntityPresenter>(missing);
            for (var i = 0; i < missing; i++) taken.Add(pool.Get());
            foreach (var bullet in taken) pool.Release(bullet);
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

        static void ClearPoolsOf(Scene unloaded)
        {
            var targets = new List<BulletEntityPresenter>();
            foreach (var (prefab, scene) in poolScenes)
            {
                if (scene == unloaded || !scene.IsValid()) targets.Add(prefab);
            }

            foreach (var prefab in targets)
            {
                if (sharedPools.TryGetValue(prefab, out var shared)) shared.Clear();
                sharedPools.Remove(prefab);
                poolScenes.Remove(prefab);
            }
        }
    }
}
