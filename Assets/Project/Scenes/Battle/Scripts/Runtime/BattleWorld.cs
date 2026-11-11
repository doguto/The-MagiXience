using System;
using System.Collections.Generic;
using Project.Scenes.Battle.Scripts.Model;
using Project.Scenes.Battle.Scripts.Presenter;

namespace Project.Scenes.Battle.Scripts.Runtime
{
    public interface IBattleTickable
    {
        bool IsFinished { get; }
        void BattleTick();
    }

    // バトル全体で共有する時計と乱数。固定 60fps で進む。
    public sealed class BattleWorld
    {
        public const int FramesPerSecond = 60;
        public const float DeltaTime = 1f / FramesPerSecond;

        readonly Func<float> bgmTimeProvider;
        readonly List<IBattleTickable> tickables = new();
        readonly List<IBattleTickable> pending = new();

        public BattleWorld(int seed, Func<float> bgmTimeProvider = null)
        {
            Random = new Random(seed);
            this.bgmTimeProvider = bgmTimeProvider;
            Services = new BattleServices();
        }

        public int Frame { get; private set; }
        public Random Random { get; }
        public float BgmTime => bgmTimeProvider?.Invoke() ?? 0f;
        public BattleServices Services { get; }

        // 追加はそのフレームの終わりに反映する (Tick 中に敵が生まれても列挙が壊れないように)
        public void Register(IBattleTickable tickable)
        {
            if (tickable != null) pending.Add(tickable);
        }

        public void Step()
        {
            FlushPending();
            for (var i = 0; i < tickables.Count; i++)
            {
                var t = tickables[i];
                if (!t.IsFinished) t.BattleTick();
            }
            tickables.RemoveAll(t => t.IsFinished);

            Services.Bullets.Tick(DeltaTime);
            BattleWind.Tick(DeltaTime);
            AdvanceFrame();
        }

        public void Clear()
        {
            tickables.Clear();
            pending.Clear();
        }

        internal void AdvanceFrame() => Frame++;

        void FlushPending()
        {
            if (pending.Count == 0) return;
            tickables.AddRange(pending);
            pending.Clear();
        }
    }

    // ノードから使うバトル内のサービス。BattleScenePresenter が設定する。
    public sealed class BattleServices
    {
        public BulletSystem Bullets { get; } = new();
        public ActorSpawner Spawner { get; set; }
        public BulletClearReceiver Clearer { get; set; }
        public BattleActor Boss { get; set; }
    }
}
