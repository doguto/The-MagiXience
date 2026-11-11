using System.Collections.Generic;
using Project.Scenes.Battle.Scripts.BehaviorTree;
using Project.Scenes.Battle.Scripts.Model.Entity;
using Project.Scenes.Battle.Scripts.Presenter.Entity;
using Project.Scenes.Battle.Scripts.Runtime.Motion;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Runtime
{
    // 敵/ボスに BehaviorTree を持たせて固定フレームで動かす。生成時に ActorSpawner が付与する。
    [DisallowMultipleComponent]
    public sealed class BattleActor : MonoBehaviour, IBattleTickable
    {
        BehaviorTreeRunner runner;
        bool destroyed;

        public BattleWorld World { get; private set; }
        public EnemyEntityModel Model { get; private set; }
        public BossEntityModel BossModel => Model as BossEntityModel;
        public bool IsBoss => Model is BossEntityModel;
        public IMotionBody Body { get; private set; }
        public int StartFrame { get; private set; }
        // この敵が動き出してからの秒数。発射時刻で変わる弾 (頂点の揺らぎなど) に使う
        public float AttackTime => (World.Frame - StartFrame) * BattleWorld.DeltaTime;
        public bool IsAlive => !destroyed && this != null && (Model == null || Model.IsAlive);

        public bool IsFinished => !IsAlive || runner == null || runner.IsFinished;

        public static BattleActor Attach(GameObject target, BattleWorld world, BehaviorTreeAsset tree, IReadOnlyList<TreeParameter> overrides = null)
        {
            var actor = target.GetComponent<BattleActor>() ?? target.AddComponent<BattleActor>();
            actor.Setup(world, tree, overrides);
            return actor;
        }

        void Setup(BattleWorld world, BehaviorTreeAsset tree, IReadOnlyList<TreeParameter> overrides)
        {
            World = world;
            Body = new TransformBody(transform);
            StartFrame = world.Frame;
            Model = TryGetComponent<BossEntityPresenter>(out var boss) ? boss.Model
                : TryGetComponent<EnemyEntityPresenter>(out var enemy) ? enemy.Model
                : null;

            runner?.Stop();
            runner = tree != null ? new BehaviorTreeRunner(tree, new BattleContext(world, this), overrides) : null;
            if (runner != null) world.Register(this);
        }

        public void BattleTick()
        {
            if (!IsAlive)
            {
                runner?.Stop();
                return;
            }
            runner.Tick();
        }

        public void Stop() => runner?.Stop();

        void OnDestroy()
        {
            destroyed = true;
            runner?.Stop();
        }
    }
}
