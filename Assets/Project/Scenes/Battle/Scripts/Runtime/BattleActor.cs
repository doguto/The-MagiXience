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
        MotionSequence movement;
        IReadOnlyList<MotionStep> movementSteps;
        bool registered;

        public BattleWorld World { get; private set; }
        public EnemyEntityModel Model { get; private set; }
        public BossEntityModel BossModel => Model as BossEntityModel;
        public bool IsBoss => Model is BossEntityModel;
        public IMotionBody Body { get; private set; }
        public int StartFrame { get; private set; }
        // この敵が動き出してからの秒数。発射時刻で変わる弾 (頂点の揺らぎなど) に使う
        public float AttackTime => (World.Frame - StartFrame) * BattleWorld.DeltaTime;
        public bool IsAlive => !destroyed && this != null && (Model == null || Model.IsAlive);

        public bool IsFinished => !IsAlive || ((runner == null || runner.IsFinished) && (movement == null || movement.IsFinished));

        // ツリーとは独立して流し続ける移動。continueIfSame なら同じ移動が再生中のとき作り直さない
        public void SetMovement(IReadOnlyList<MotionStep> steps, bool continueIfSame = false)
        {
            if (continueIfSame && ReferenceEquals(steps, movementSteps) && movement != null && !movement.IsFinished) return;
            movement?.Stop();
            movementSteps = steps;
            movement = steps == null || steps.Count == 0 ? null
                : new MotionSequence(steps, Body, new MotionLaunch { AttackTime = AttackTime });
        }

        // ツリーが最初に動く前に呼ばれる。付与先の部品 (進行ゲージなど) が購読の準備に使う
        public static event System.Action<BattleActor> Attached;

        public static BattleActor Attach(GameObject target, BattleWorld world, BehaviorTreeAsset tree, IReadOnlyList<TreeParameter> overrides = null)
        {
            var actor = target.GetComponent<BattleActor>();
            if (actor == null) actor = target.AddComponent<BattleActor>();
            actor.Setup(world, tree, overrides);
            Attached?.Invoke(actor);
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
            if (registered) return;
            registered = true;
            world.Register(this);
        }

        public void BattleTick()
        {
            if (!IsAlive)
            {
                runner?.Stop();
                return;
            }
            movement?.Tick(BattleWorld.DeltaTime);
            if (runner != null && !runner.IsFinished) runner.Tick();
        }

        public void Stop()
        {
            runner?.Stop();
            movement?.Stop();
            movement = null;
        }

        void OnDestroy()
        {
            destroyed = true;
            runner?.Stop();
            movement?.Stop();
        }
    }
}
