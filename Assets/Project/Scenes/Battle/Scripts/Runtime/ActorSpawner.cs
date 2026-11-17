using Project.Scenes.Battle.Scripts.BehaviorTree;
using Project.Scenes.Battle.Scripts.Presenter;
using Project.Scenes.Battle.Scripts.Presenter.Entity;
using Project.Scenes.Battle.Scripts.View;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Runtime
{
    // 敵・ビーム・障害物などの Prefab を出現させる。
    public sealed class ActorSpawner
    {
        readonly BattleWorld world;
        readonly EnemyTracker tracker;
        readonly ActorPool pool;

        public ActorSpawner(BattleWorld world, EnemyTracker tracker, ActorPool pool = null)
        {
            this.world = world;
            this.tracker = tracker;
            this.pool = pool;
        }

        public int ActiveEnemyCount => tracker != null ? tracker.ActiveEnemyCount : 0;

        public GameObject Spawn(in ActorSpawn spawn)
        {
            if (spawn.Prefab == null)
            {
                Debug.LogWarning("[ActorSpawner] Prefab is not assigned.");
                return null;
            }

            var rotation = spawn.Rotation == default ? Quaternion.identity : spawn.Rotation;
            var instance = pool != null && ActorPool.IsPoolable(spawn.Prefab)
                ? pool.Get(spawn.Prefab, spawn.Position, rotation)
                : Object.Instantiate(spawn.Prefab, spawn.Position, rotation);

            if (instance.TryGetComponent<IBeamVisualReceiver>(out var beam))
            {
                beam.ConfigureBeam(spawn.BeamRange, spawn.BeamDuration, spawn.BeamWidth);
            }

            var enemy = instance.GetComponent<EnemyEntityPresenter>();
            if (spawn.Behavior != null) BattleActor.Attach(instance, world, spawn.Behavior, spawn.Overrides);

            if (enemy != null && spawn.Tracked && tracker != null)
            {
                tracker.RegisterEnemy(enemy);
            }

            return instance;
        }
    }
}
