using System;
using System.Collections.Generic;
using Project.Scenes.Battle.Scripts.BehaviorTree;
using Project.Scenes.Battle.Scripts.Model;
using Project.Scenes.Battle.Scripts.Presenter;
using Project.Scenes.Battle.Scripts.Runtime.Motion;
using Project.Scripts.Extensions;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Runtime
{
    // バトル 1 回分 (道中またはボス戦) の BehaviorTree 実行を管理する。
    // シナリオ遷移・BGM・リトライなどシーンをまたぐ処理は BattleScenePresenter に残す。
    public sealed class BattleFlow
    {
        readonly BattleClock clock;
        readonly EnemyTracker tracker;
        readonly BulletClearReceiver clearer;
        readonly Func<AudioSource> bgmSource;
        readonly Action<SeType> playSe;
        BattleWorld world;
        MotionTickable entrance;

        public BattleFlow(BattleClock clock, EnemyTracker tracker, BulletClearReceiver clearer, Func<AudioSource> bgmSource, Action<SeType> playSe)
        {
            this.clock = clock;
            this.tracker = tracker;
            this.clearer = clearer;
            this.bgmSource = bgmSource;
            this.playSe = playSe;
        }

        public BattleWorld World => world;
        public event Action<string> PhaseStarted;

        // 道中などステージ直下のツリーを開始する。完了したら onFinished を呼ぶ
        public void StartStageTree(BehaviorTreeAsset tree, Action onFinished)
        {
            if (tree == null)
            {
                onFinished?.Invoke();
                return;
            }

            EnsureWorld();
            var runner = new BehaviorTreeRunner(tree, new BattleContext(world));
            runner.Finished += _ => onFinished?.Invoke();
            world.Register(new RunnerTickable(runner));
        }

        public void PlayEntrance(Transform boss, IReadOnlyList<MotionStep> steps)
        {
            if (boss == null || steps == null || steps.Count == 0) return;
            EnsureWorld();
            entrance?.Stop();
            entrance = new MotionTickable(new MotionSequence(steps, new TransformBody(boss), default));
            world.Register(entrance);
        }

        public BattleActor StartBossTree(GameObject boss, BehaviorTreeAsset tree)
        {
            if (boss == null) return null;
            EnsureWorld();
            entrance?.Stop();
            entrance = null;

            var actor = BattleActor.Attach(boss, world, tree);
            world.Services.Boss = actor;
            return actor;
        }

        public void Stop()
        {
            entrance?.Stop();
            entrance = null;
            if (world != null)
            {
                world.Services.Bullets.Clear();
                world.Services.PhaseStarted -= OnPhaseStarted;
            }
            clock.End();
            world = null;
            BattleWind.Reset();
        }

        void EnsureWorld()
        {
            if (world != null) return;

            world = new BattleWorld(Environment.TickCount, () => bgmSource?.Invoke() is { } s ? s.time : 0f);
            var services = world.Services;
            services.Spawner = new ActorSpawner(world, tracker);
            services.Clearer = clearer;
            services.PlaySe = playSe;
            services.BgmSource = bgmSource?.Invoke();
            services.PhaseStarted += OnPhaseStarted;
            BattleWind.Reset();
            clock.Begin(world);
        }

        void OnPhaseStarted(string phaseId) => PhaseStarted?.Invoke(phaseId);

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
        }
    }
}
