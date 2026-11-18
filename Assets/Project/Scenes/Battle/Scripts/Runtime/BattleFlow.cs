using System;
using Project.Scenes.Battle.Scripts.BehaviorTree;
using Project.Scenes.Battle.Scripts.Model;
using Project.Scenes.Battle.Scripts.Presenter;
using Project.Scenes.Battle.Scripts.Presenter.Entity;
using Project.Scenes.Battle.Scripts.Runtime.Motion;
using Project.Scripts.Extensions;
using Project.Scripts.Model;
using UniRx;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Runtime
{
    // バトル 1 回分 (1 ステージ) の BehaviorTree 実行を管理する。ボスの生成と戦闘開始もここが受け持つ。
    // シナリオ遷移・BGM・リトライなどシーンをまたぐ処理は BattleScenePresenter が、このクラスのイベントに反応して行う。
    public sealed class BattleFlow : IBossHost
    {
        readonly BattleClock clock;
        readonly EnemyTracker tracker;
        readonly BulletClearReceiver clearer;
        readonly Func<AudioSource> bgmSource;
        readonly Action<SeType> playSe;
        readonly CompositeDisposable bossSubscriptions = new();
        readonly CompositeDisposable worldSubscriptions = new();
        readonly Subject<NodeStatus> stageFinished = new();
        readonly Subject<(int Index, BattleSituation Situation, bool Resumed)> sectionStarted = new();
        readonly Subject<BossEntityPresenter> bossSpawned = new();
        readonly Subject<BossEntityPresenter> bossBattleStarted = new();
        BattleWorld world;
        BehaviorTreeRunner stageRunner;
        EnemyPool enemyPool;
        MotionTickable entrance;
        BossEntityPresenter boss;
        BossDefinition bossDefinition;
        bool bossDefeated;

        public BattleFlow(BattleClock clock, EnemyTracker tracker, BulletClearReceiver clearer, Func<AudioSource> bgmSource, Action<SeType> playSe)
        {
            this.clock = clock;
            this.tracker = tracker;
            this.clearer = clearer;
            this.bgmSource = bgmSource;
            this.playSe = playSe;
        }

        public BattleWorld World => world;
        public PlayerEntityPresenter Player { get; set; }
        public BackgroundPresenter Background { get; set; }
        public IScenarioService Scenario { get; set; }
        public Action<BgmType> PlayBgm { get; set; }
        public BossEntityPresenter SpawnedBoss => boss;

        public IObservable<NodeStatus> OnStageFinished => stageFinished;
        public IObservable<(int Index, BattleSituation Situation, bool Resumed)> OnSectionStarted => sectionStarted;
        public IObservable<BossEntityPresenter> OnBossSpawned => bossSpawned;
        public IObservable<BossEntityPresenter> OnBossBattleStarted => bossBattleStarted;

        // ステージのツリーを startSection から始める。resume ならその Section の会話を飛ばす
        public void StartStage(BattleStageAsset stage, int startSection, bool resume)
        {
            EnsureWorld();
            world.SetStage(new StageContext(world, stage != null ? stage.Services : null));
            world.Services.Progress.StartSection = startSection;
            world.Services.Progress.ResumeStart = resume;

            if (stage == null || stage.Tree == null)
            {
                Debug.LogError("[BattleFlow] BattleStageAsset または Tree が設定されていません");
                return;
            }

            Prewarm(stage);

            stageRunner = new BehaviorTreeRunner(stage.Tree, new BattleContext(world));
            stageRunner.OnFinished.Subscribe(stageFinished.OnNext).AddTo(worldSubscriptions);
            world.Register(new RunnerTickable(stageRunner));
        }

        // 1 種類あたりに先に作っておく数。同時に出る数の目安で、足りなければプールが増やす
        const int BulletPrewarmCount = 30;
        const int EnemyPrewarmCount = 4;

        void Prewarm(BattleStageAsset stage)
        {
            var collected = StagePrefabCollector.Collect(stage);
            foreach (var bullet in collected.Bullets) Presenter.Entity.BulletPool.Prewarm(bullet, BulletPrewarmCount);
            foreach (var prefab in collected.Spawnables)
            {
                if (EnemyPool.IsPoolable(prefab)) enemyPool.Prewarm(prefab, EnemyPrewarmCount);
            }
        }

        public void Stop()
        {
            // 実行中の会話などを Abort で後始末させる
            stageRunner?.Stop();
            stageRunner = null;
            entrance?.Stop();
            entrance = null;
            bossSubscriptions.Clear();
            if (boss != null) UnityEngine.Object.Destroy(boss.gameObject);
            boss = null;
            bossDefinition = null;
            bossDefeated = false;

            if (world != null)
            {
                world.Services.Bullets.Clear();
                world.Clear();
            }
            worldSubscriptions.Clear();
            clock.End();
            world = null;
            Player?.BindBulletSystem(null);
            clearer?.BindBulletSystem(null);
        }

        // ---------- IBossHost ----------

        public BossDefinition Current => bossDefinition;
        public bool IsDefeated => bossDefeated;

        public bool Spawn(BossDefinition definition)
        {
            if (definition == null || definition.Prefab == null)
            {
                Debug.LogError("[BattleFlow] BossDefinition の Prefab が設定されていません");
                return false;
            }
            if (boss != null)
            {
                Debug.LogWarning("[BattleFlow] ボスは既に出ています", boss);
                return false;
            }

            EnsureWorld();
            var instance = UnityEngine.Object.Instantiate(definition.Prefab, definition.SpawnPosition, Quaternion.identity);
            boss = instance.GetComponent<BossEntityPresenter>();
            if (boss == null)
            {
                Debug.LogError("[BattleFlow] ボスの Prefab に BossEntityPresenter がありません", instance);
                UnityEngine.Object.Destroy(instance);
                return false;
            }

            bossDefinition = definition;
            bossDefeated = false;
            boss.OnDeath.Take(1).Subscribe(_ => world?.Services.Boss?.Stop()).AddTo(bossSubscriptions);
            boss.OnDeathSequenceCompleted.Take(1).Subscribe(_ => bossDefeated = true).AddTo(bossSubscriptions);

            if (definition.Entrance != null && definition.Entrance.Count > 0)
            {
                entrance = new MotionTickable(new MotionSequence(definition.Entrance, new TransformBody(boss.transform), default));
                world.Register(entrance);
            }

            bossSpawned.OnNext(boss);
            return true;
        }

        // デバッグ起動で、ボス戦を途中のフェーズ・HP から始める
        public string DebugBossStartPhase { get; set; }
        public float DebugBossStartHpPercent { get; set; } = 100f;

        public EnemyAgent StartBattle(EnemyTreeAsset tree)
        {
            if (boss == null || tree == null) return null;

            world.Services.SkipToPhase = DebugBossStartPhase;
            if (DebugBossStartHpPercent < 100f) boss.Model.SetHpPercent(DebugBossStartHpPercent);

            // 旧実装と同じく、登場の動きは途中で止めず終点まで一気に進める
            entrance?.Complete();
            entrance = null;

            var agent = EnemyAgent.Attach(boss.gameObject, world, tree);
            world.Services.Boss = agent;
            bossBattleStarted.OnNext(boss);
            return agent;
        }

        void EnsureWorld()
        {
            if (world != null) return;

            world = new BattleWorld(Environment.TickCount, () => bgmSource?.Invoke() is { } s ? s.time : 0f);
            // プールはリトライをまたいで使い回す。Battle シーンと一緒に破棄される
            if (enemyPool == null) enemyPool = EnemyPool.Create();
            var services = world.Services;
            services.Spawner = new EnemySpawner(world, tracker, enemyPool);
            services.Clearer = clearer;
            services.PlaySe = playSe;
            services.PlayBgm = type => PlayBgm?.Invoke(type);
            services.BgmSource = bgmSource?.Invoke();
            services.BossHost = this;
            services.Scenario = Scenario;
            services.Player = Player;
            services.Background = Background;
            Player?.BindBulletSystem(services.Bullets);
            clearer?.BindBulletSystem(services.Bullets);
            services.Progress.OnSectionStarted.Subscribe(sectionStarted.OnNext).AddTo(worldSubscriptions);
            clock.Begin(world);
        }

        sealed class RunnerTickable : IBattleTickable
        {
            readonly BehaviorTreeRunner runner;
            public RunnerTickable(BehaviorTreeRunner runner) => this.runner = runner;
            public bool IsFinished => runner.IsFinished;
            public void BattleTick() => runner.Tick();
        }

        sealed class MotionTickable : IBattleTickable
        {
            readonly MotionSequence sequence;
            bool stopped;

            public MotionTickable(MotionSequence sequence) => this.sequence = sequence;
            public bool IsFinished => stopped || sequence.IsFinished;
            public void BattleTick() => sequence.Tick(BattleWorld.DeltaTime);

            public void Stop()
            {
                stopped = true;
                sequence.Stop();
            }

            public void Complete()
            {
                for (var i = 0; i < 64 && !sequence.IsFinished; i++) sequence.Tick(3600f);
                Stop();
            }
        }
    }
}
