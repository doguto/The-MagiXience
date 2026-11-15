using System;
using System.Collections.Generic;
using Project.Scenes.Battle.Scripts.Presenter.Entity;
using Project.Scenes.Battle.Scripts.View;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Runtime
{
    // プールから出し入れされるときに状態を戻すコンポーネントが実装する
    public interface IPoolableActor
    {
        // 2 回目以降に出されたとき (初回は Awake で初期化されるので呼ばない)
        void OnTakenFromPool();
        void OnReturnedToPool();
    }

    // 敵を消すときの共通の入口。プールから出したものはプールへ戻し、それ以外は Destroy する
    public static class ActorDespawn
    {
        public static void Despawn(GameObject actor)
        {
            if (actor == null) return;
            if (actor.TryGetComponent<PooledActor>(out var pooled) && pooled.Pool != null) pooled.Pool.Release(actor);
            else UnityEngine.Object.Destroy(actor);
        }
    }

    // Spawn ノードで出す敵を Prefab ごとに使い回す。Battle シーンに 1 つ置き、シーンと一緒に破棄される
    public sealed class ActorPool : MonoBehaviour
    {
        // 独自の状態を Start などで一度だけ作るため、使い回すと動きが変わるもの
        static readonly Type[] Unsupported =
        {
            typeof(BossEntityPresenter),
            typeof(TwinParabolaMidBossController),
            typeof(DamageRelayToBoss),
            typeof(StaffNotationPresenter),
        };

        static readonly Dictionary<GameObject, bool> poolableCache = new();

        readonly Dictionary<GameObject, Stack<GameObject>> idle = new();
        readonly List<GameObject> pendingRelease = new();
        Transform storage;

        public static ActorPool Create()
        {
            var go = new GameObject("ActorPool");
            var pool = go.AddComponent<ActorPool>();
            var storage = new GameObject("Storage");
            // 非アクティブの親の下で生成すると、出すまで Awake が走らない (事前生成で旧来の初期化を動かさない)
            storage.SetActive(false);
            storage.transform.SetParent(go.transform, false);
            pool.storage = storage.transform;
            return pool;
        }

        public static bool IsPoolable(GameObject prefab)
        {
            if (prefab == null) return false;
            if (poolableCache.TryGetValue(prefab, out var cached)) return cached;

            var result = prefab.TryGetComponent<EnemyEntityPresenter>(out _) && prefab.GetComponentInChildren<NoActorPool>(true) == null;
            foreach (var type in Unsupported)
            {
                if (result && prefab.GetComponentInChildren(type, true) != null) result = false;
            }
            poolableCache[prefab] = result;
            return result;
        }

        public GameObject Get(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            if (idle.TryGetValue(prefab, out var stack))
            {
                while (stack.Count > 0)
                {
                    var go = stack.Pop();
                    if (go != null) return Activate(go, position, rotation);
                }
            }

            var created = Instantiate(prefab, position, rotation);
            Tag(created, prefab).HasBeenActive = true;
            return created;
        }

        public void Prewarm(GameObject prefab, int count)
        {
            if (prefab == null || count <= 0) return;
            if (!idle.TryGetValue(prefab, out var stack)) idle[prefab] = stack = new Stack<GameObject>();
            for (var i = stack.Count; i < count; i++)
            {
                var go = Instantiate(prefab, storage);
                Tag(go, prefab);
                stack.Push(go);
            }
        }

        // Destroy と同じくフレームの最後に戻す。撃破の通知の途中で戻すと、後続の購読者 (撃破時の行動) が壊れるため
        public void Release(GameObject actor)
        {
            if (actor != null && !pendingRelease.Contains(actor)) pendingRelease.Add(actor);
        }

        void LateUpdate()
        {
            if (pendingRelease.Count == 0) return;
            foreach (var go in pendingRelease)
            {
                if (go == null) continue;
                foreach (var poolable in go.GetComponentsInChildren<IPoolableActor>(true)) poolable.OnReturnedToPool();
                go.SetActive(false);
                go.transform.SetParent(storage, false);

                var pooled = go.GetComponent<PooledActor>();
                if (!idle.TryGetValue(pooled.Prefab, out var stack)) idle[pooled.Prefab] = stack = new Stack<GameObject>();
                stack.Push(go);
            }
            pendingRelease.Clear();
        }

        GameObject Activate(GameObject go, Vector3 position, Quaternion rotation)
        {
            var pooled = go.GetComponent<PooledActor>();
            var reused = pooled.HasBeenActive;
            // 親を外すと、プールの置かれたシーン (Battle) のルートに戻る
            go.transform.SetParent(null, false);
            go.transform.SetPositionAndRotation(position, rotation);
            go.SetActive(true);
            pooled.HasBeenActive = true;
            if (reused)
            {
                foreach (var poolable in go.GetComponentsInChildren<IPoolableActor>(true)) poolable.OnTakenFromPool();
            }
            return go;
        }

        PooledActor Tag(GameObject go, GameObject prefab)
        {
            var pooled = go.AddComponent<PooledActor>();
            pooled.Prefab = prefab;
            pooled.Pool = this;
            return pooled;
        }

        void OnDestroy()
        {
            idle.Clear();
            pendingRelease.Clear();
        }
    }
}
