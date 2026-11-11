using System;
using System.Collections.Generic;
using Project.Scenes.Battle.Scripts.Model;
using Project.Scenes.Battle.Scripts.Presenter;
using Project.Scripts.Extensions;

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
        readonly List<Scheduled> scheduled = new();
        readonly List<Scheduled> due = new();

        struct Scheduled
        {
            public int Frame;
            public int Order;
            public BattleActor Owner;
            public bool HasOwner;
            public Action Action;
        }

        int scheduleOrder;

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

        // frame 後に action を実行する。owner を渡すと、その敵が倒れた/消えた時点で取り消す
        public void Schedule(int delayFrames, Action action, BattleActor owner = null)
        {
            if (action == null) return;
            if (delayFrames <= 0 && (owner == null || owner.IsAlive))
            {
                action();
                return;
            }
            scheduled.Add(new Scheduled { Frame = Frame + delayFrames, Order = scheduleOrder++, Owner = owner, HasOwner = owner != null, Action = action });
        }

        public void CancelScheduled(BattleActor owner)
        {
            scheduled.RemoveAll(s => s.HasOwner && s.Owner == owner);
        }

        public void Step()
        {
            RunScheduled();
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
            scheduled.Clear();
        }

        void RunScheduled()
        {
            if (scheduled.Count == 0) return;
            due.Clear();
            for (var i = scheduled.Count - 1; i >= 0; i--)
            {
                if (scheduled[i].Frame > Frame) continue;
                due.Add(scheduled[i]);
                scheduled.RemoveAt(i);
            }
            due.Sort((a, b) => a.Frame != b.Frame ? a.Frame.CompareTo(b.Frame) : a.Order.CompareTo(b.Order));
            foreach (var s in due)
            {
                if (s.HasOwner && (s.Owner == null || !s.Owner.IsAlive)) continue;
                s.Action();
            }
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
        public Action<SeType> PlaySe { get; set; }
        public UnityEngine.AudioSource BgmSource { get; set; }

        // PhaseMarker ノードが通知するフェーズの区切り (進行ゲージなどが購読する)
        public event Action<string> PhaseStarted;
        public void NotifyPhase(string phaseId) => PhaseStarted?.Invoke(phaseId);
    }
}
