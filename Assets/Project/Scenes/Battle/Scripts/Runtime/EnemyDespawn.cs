using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Runtime
{
    // 敵を消すときの共通の入口。プールから出したものはプールへ戻し、それ以外は Destroy する
    public static class EnemyDespawn
    {
        public static void Despawn(GameObject enemy)
        {
            if (enemy == null) return;
            if (enemy.TryGetComponent<PooledEnemy>(out var pooled) && pooled.Pool != null) pooled.Pool.Release(enemy);
            else UnityEngine.Object.Destroy(enemy);
        }
    }
}
