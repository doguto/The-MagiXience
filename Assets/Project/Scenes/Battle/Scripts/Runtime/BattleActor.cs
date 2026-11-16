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
    public sealed class BattleActor : MonoBehaviour, IBattleTickable, IPoolableActor
    {
        BehaviorTreeRunner runner;
        bool destroyed;
        // プールへ戻された。次に Attach されるまで生きていない扱いにする
        bool released;
        BehaviorTreeAsset deathBehavior;
        System.IDisposable deathSubscription;

        public BattleWorld World { get; private set; }
        public EnemyEntityModel Model { get; private set; }
        public BossEntityModel BossModel => Model as BossEntityModel;
        public bool IsBoss => Model is BossEntityModel;
        public IMotionBody Body { get; private set; }
        public int StartFrame { get; private set; }
        // この敵が動き出してからの秒数。発射時刻で変わる弾 (頂点の揺らぎなど) に使う
        public float AttackTime => (World.Frame - StartFrame) * BattleWorld.DeltaTime;
        public bool IsAlive => !destroyed && !released && this != null && (Model == null || Model.IsAlive);

        public bool IsFinished => !IsAlive || runner == null || runner.IsFinished;

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

            deathSubscription?.Dispose();
            deathSubscription = Model != null ? UniRx.ObservableExtensions.Subscribe(Model.OnDeath, _ => RunDeathBehavior()) : null;

            released = false;
            runner?.Stop();
            runner = tree != null ? new BehaviorTreeRunner(tree, new BattleContext(world, this), overrides) : null;
            world.Register(this);
        }

        public void OnTakenFromPool() { }

        public void OnReturnedToPool()
        {
            released = true;
            World?.CancelScheduled(this);
            runner?.Stop();
            runner = null;
            deathBehavior = null;
            deathSubscription?.Dispose();
            deathSubscription = null;
        }

        public void BattleTick()
        {
            if (!IsAlive)
            {
                runner?.Stop();
                return;
            }
            if (runner != null && !runner.IsFinished) runner.Tick();
        }

        // 倒されたときに 1 回だけ実行するツリー (旧「死亡時攻撃」)
        public void SetDeathBehavior(BehaviorTreeAsset tree) => deathBehavior = tree;

        void RunDeathBehavior()
        {
            if (deathBehavior == null || World == null) return;
            var once = new BehaviorTreeRunner(deathBehavior, new BattleContext(World, this));
            once.Tick();
            once.Stop();
        }

        public void Stop()
        {
            runner?.Stop();
        }

        void OnDestroy()
        {
            destroyed = true;
            deathSubscription?.Dispose();
            runner?.Stop();
        }
    }
}
