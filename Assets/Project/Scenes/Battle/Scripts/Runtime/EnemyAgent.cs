using System;
using System.Collections.Generic;
using Project.Scenes.Battle.Scripts.BehaviorTree;
using Project.Scenes.Battle.Scripts.Model;
using Project.Scenes.Battle.Scripts.Model.Entity;
using Project.Scenes.Battle.Scripts.Presenter.Entity;
using Project.Scenes.Battle.Scripts.Runtime.Motion;
using UniRx;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Runtime
{
    // 敵/ボスに BehaviorTree を持たせて固定フレームで動かし、ツリーの Self になる。生成時に EnemySpawner が付与する。
    // 雑魚が寿命・画面外・撃破で消えるかもここで判定する。ボスの撃破は BossEntityPresenter の演出が片付ける
    [DisallowMultipleComponent]
    public sealed class EnemyAgent : MonoBehaviour, IAgent, IBattleTickable, IPoolableEnemy
    {
        BehaviorTreeRunner runner;
        bool destroyed;
        // プールへ戻された。次に Attach されるまで生きていない扱いにする
        bool released;
        IDisposable deathSubscription;
        EnemyEntityPresenter presenter;
        DespawnBehavior despawnBehavior;
        DespawnReason pendingReason;
        bool despawnCompleted;
        bool enteredScreen;
        int lifetimeFrames;

        public BattleWorld World { get; private set; }
        public EnemyEntityModel Model { get; private set; }
        public BossEntityModel BossModel => Model as BossEntityModel;
        public bool IsBoss => Model is BossEntityModel;
        public IMotionBody Body { get; private set; }
        public IReadOnlyList<MotionStep> Motion { get; private set; }
        public Vector2 Direction => Vector2.zero;
        public int StartFrame { get; private set; }
        // この敵が動き出してからの秒数。発射時刻で変わる弾 (頂点の揺らぎなど) に使う
        public float AttackTime => (World.Frame - StartFrame) * BattleWorld.DeltaTime;
        public bool IsAlive => !destroyed && !released && this != null && pendingReason == DespawnReason.None && (Model == null || Model.IsAlive);

        public int Serial { get; private set; }
        public IAgent Owner { get; private set; }
        public int OwnerSerial { get; private set; }
        public string Group { get; private set; }
        public SignalInbox Signals { get; } = new();

        public bool IsFinished => destroyed || released || despawnCompleted || this == null;

        // ツリーが最初に動く前に呼ばれる。付与先の部品 (進行ゲージなど) が購読の準備に使う
        static readonly Subject<EnemyAgent> attached = new();
        public static IObservable<EnemyAgent> OnAttached => attached;

        public static EnemyAgent Attach(GameObject target, BattleWorld world, EnemyTreeAsset tree, IReadOnlyList<TreeParameter> overrides = null, IReadOnlyList<MotionStep> motion = null,
            IAgent owner = null, string group = null)
        {
            var agent = target.GetComponent<EnemyAgent>();
            if (agent == null) agent = target.AddComponent<EnemyAgent>();
            agent.Setup(world, tree, overrides, motion, owner, group);
            attached.OnNext(agent);
            return agent;
        }

        void Setup(BattleWorld world, EnemyTreeAsset tree, IReadOnlyList<TreeParameter> overrides, IReadOnlyList<MotionStep> motion, IAgent owner, string group)
        {
            World = world;
            Serial = world.NextSerial();
            Owner = owner;
            OwnerSerial = owner?.Serial ?? 0;
            Group = group;
            Signals.Clear();
            Body = new TransformBody(transform);
            Motion = motion;
            StartFrame = world.Frame;
            var boss = GetComponent<BossEntityPresenter>();
            presenter = boss == null ? GetComponent<EnemyEntityPresenter>() : null;
            Model = boss != null ? boss.Model : presenter != null ? presenter.Model : null;
            lifetimeFrames = presenter != null && presenter.LifetimeSeconds > 0f ? BattleContext.SecondsToFrames(presenter.LifetimeSeconds) : 0;

            released = false;
            despawnCompleted = false;
            enteredScreen = false;
            pendingReason = DespawnReason.None;
            despawnBehavior = default;

            deathSubscription?.Dispose();
            deathSubscription = Model != null ? Model.OnDeath.Subscribe(_ => MarkDespawn(DespawnReason.Defeated)) : null;

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
            Owner = null;
            despawnBehavior = default;
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
            if (!IsAlive || presenter == null) return;

            if (lifetimeFrames > 0 && World.Frame - StartFrame + 1 >= lifetimeFrames) MarkDespawn(DespawnReason.Expired);
            else if (IsOutOfScreen()) MarkDespawn(DespawnReason.OutOfScreen);
        }

        // 画面の外から出てくる敵もいるため、一度画面に入ってから外へ出たときだけ消す
        bool IsOutOfScreen()
        {
            var position = transform.position;
            var margin = presenter.ScreenMargin;
            var outOfScreen = position.x < ScreenBoundsCache.MinX - margin || position.x > ScreenBoundsCache.MaxX + margin ||
                              position.y < ScreenBoundsCache.MinY - margin || position.y > ScreenBoundsCache.MaxY + margin;
            if (!outOfScreen) enteredScreen = true;
            return enteredScreen && outOfScreen;
        }

        public void Despawn() => MarkDespawn(DespawnReason.Despawned);

        public void SetDespawnBehavior(BehaviorTreeAsset tree, DespawnReason reasons) =>
            despawnBehavior = new DespawnBehavior { Tree = tree, Reasons = reasons };

        // 撃破は物理演算の中 (Step の外) で起きるため、印を付けるだけにして BattleWorld が Step の中で片付ける
        void MarkDespawn(DespawnReason reason)
        {
            if (pendingReason != DespawnReason.None || destroyed || released || World == null) return;
            pendingReason = reason;
            World.RequestDespawn(this);
        }

        internal void CompleteDespawn()
        {
            if (despawnCompleted || destroyed || released) return;
            despawnCompleted = true;
            runner?.Stop();
            despawnBehavior.Run(this, pendingReason);
            if (presenter == null && pendingReason == DespawnReason.Defeated) return;
            World.CancelScheduled(this);
            EnemyDespawn.Despawn(gameObject);
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
