using Project.Scenes.Battle.Scripts.BehaviorTree;
using Project.Scenes.Battle.Scripts.Presenter;
using Project.Scenes.Battle.Scripts.Presenter.Entity;
using Project.Scenes.Battle.Scripts.View;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Runtime
{
    // 敵・ビーム・障害物などの Prefab を出現させる。
    public sealed class EnemySpawner
    {
        readonly BattleWorld world;
        readonly EnemyTracker tracker;
        readonly EnemyPool pool;

        public EnemySpawner(BattleWorld world, EnemyTracker tracker, EnemyPool pool = null)
        {
            this.world = world;
            this.tracker = tracker;
            this.pool = pool;
        }

        public int ActiveEnemyCount => tracker != null ? tracker.ActiveEnemyCount : 0;

        public GameObject Spawn(in EnemySpawn spawn)
        {
            if (spawn.Prefab == null)
            {
                Debug.LogWarning("[EnemySpawner] Prefab is not assigned.");
                return null;
            }

            var rotation = spawn.Rotation == default ? Quaternion.identity : spawn.Rotation;
            var instance = pool != null && EnemyPool.IsPoolable(spawn.Prefab)
                ? pool.Get(spawn.Prefab, spawn.Position, rotation)
                : Object.Instantiate(spawn.Prefab, spawn.Position, rotation);

            if (instance.TryGetComponent<IBeamVisualReceiver>(out var beam))
            {
                beam.ConfigureBeam(spawn.BeamRange, spawn.BeamDuration, spawn.BeamWidth);
            }

            var enemy = instance.GetComponent<EnemyEntityPresenter>();
            // 敵はツリーが無くても EnemyAgent が消えるかを判定するので必ず付ける
            if (spawn.Behavior != null || enemy != null) EnemyAgent.Attach(instance, world, spawn.Behavior, spawn.Overrides, spawn.Motion, spawn.Owner, spawn.Group);

            if (enemy != null && spawn.Tracked && tracker != null)
            {
                tracker.RegisterEnemy(enemy);
            }

            return instance;
        }
    }
}
