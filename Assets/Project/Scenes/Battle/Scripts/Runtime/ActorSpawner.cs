using System.Collections.Generic;
using Project.Scenes.Battle.Scripts.BehaviorTree;
using Project.Scenes.Battle.Scripts.Presenter;
using Project.Scenes.Battle.Scripts.Presenter.Entity;
using Project.Scenes.Battle.Scripts.View;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Runtime
{
    public struct ActorSpawn
    {
        public GameObject Prefab;
        public Vector3 Position;
        public Quaternion Rotation;
        // 指定すると Prefab 側の旧来の移動・攻撃を止め、このツリーで動かす
        public BehaviorTreeAsset Behavior;
        public IReadOnlyList<TreeParameter> Overrides;
        // 「敵の全滅」の判定対象にするか
        public bool Tracked;
        // ビーム/警告線の設定。0 以下は Prefab の値
        public float BeamRange;
        public float BeamDuration;
        public float BeamWidth;
    }

    // 敵・ビーム・障害物などの Prefab を出現させる。
    public sealed class ActorSpawner
    {
        readonly BattleWorld world;
        readonly EnemyTracker tracker;

        public ActorSpawner(BattleWorld world, EnemyTracker tracker)
        {
            this.world = world;
            this.tracker = tracker;
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
            var instance = Object.Instantiate(spawn.Prefab, spawn.Position, rotation);

            if (instance.TryGetComponent<IBeamVisualReceiver>(out var beam))
            {
                beam.ConfigureBeam(spawn.BeamRange, spawn.BeamDuration, spawn.BeamWidth);
            }

            var enemy = instance.GetComponent<EnemyEntityPresenter>();
            if (spawn.Behavior != null)
            {
                if (enemy != null) enemy.DetachLegacyBehaviour();
                BattleActor.Attach(instance, world, spawn.Behavior, spawn.Overrides);
            }

            if (enemy != null && spawn.Tracked && tracker != null)
            {
                tracker.RegisterEnemy(enemy);
            }

            return instance;
        }
    }
}
