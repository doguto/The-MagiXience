using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Runtime
{
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
}
