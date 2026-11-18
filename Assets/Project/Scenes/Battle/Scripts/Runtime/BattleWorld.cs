using System;
using System.Collections.Generic;

namespace Project.Scenes.Battle.Scripts.Runtime
{
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
            public IAgent Owner;
            public bool HasOwner;
            public Action Action;
        }

        int scheduleOrder;

        public BattleWorld(int seed, Func<float> bgmTimeProvider = null)
        {
            Random = new Random(seed);
            this.bgmTimeProvider = bgmTimeProvider;
            Services = new BattleServices(this);
        }

        // 現在のステージ (道中/ボス戦) で宣言されたサービス。ステージ外では null
        public StageContext Stage { get; private set; }

        public void SetStage(StageContext stage)
        {
            Stage?.End();
            Stage = stage;
            Stage?.Begin();
        }

        public int Frame { get; private set; }
        public Random Random { get; }
        public float BgmTime => bgmTimeProvider?.Invoke() ?? 0f;
        public BattleServices Services { get; }
        // 弾や消えるときのツリーなど、頻繁に使うツリーの複製を使い回す
        public TreePool Trees { get; } = new();

        readonly List<EnemyAgent> despawning = new();
        readonly List<Signal> outgoing = new();
        readonly List<Signal> delivering = new();
        int serialCounter;

        internal int NextSerial() => ++serialCounter;

        // 送り手によらず次のフレームの頭に届ける。ツリーを処理する順番で結果が変わらないようにするため
        public void SendSignal(IAgent sender, string key, SignalTarget target, string group)
        {
            if (!string.IsNullOrEmpty(key)) outgoing.Add(new Signal(sender, key, target, group));
        }

        // 消える敵を登録する。消えるときのツリーと片付けは、このフレームの弾の処理の後にまとめて行う
        internal void RequestDespawn(EnemyAgent agent)
        {
            if (!despawning.Contains(agent)) despawning.Add(agent);
        }

        // 追加はそのフレームの終わりに反映する (Tick 中に敵が生まれても列挙が壊れないように)
        public void Register(IBattleTickable tickable)
        {
            // プールで使い回す敵は、前の回の登録が残っているうちに付け直されることがあるため重複を避ける
            if (tickable == null || pending.Contains(tickable) || tickables.Contains(tickable)) return;
            pending.Add(tickable);
        }

        // frame 後に action を実行する。owner を渡すと、その敵が倒れた/消えた時点で取り消す
        public void Schedule(int delayFrames, Action action, IAgent owner = null)
        {
            if (action == null) return;
            if (delayFrames <= 0 && (owner == null || owner.IsAlive))
            {
                action();
                return;
            }
            scheduled.Add(new Scheduled { Frame = Frame + delayFrames, Order = scheduleOrder++, Owner = owner, HasOwner = owner != null, Action = action });
        }

        public void CancelScheduled(IAgent owner)
        {
            scheduled.RemoveAll(s => s.HasOwner && s.Owner == owner);
        }

        public void Step()
        {
            DeliverSignals();
            RunScheduled();
            FlushPending();
            for (var i = 0; i < tickables.Count; i++)
            {
                var t = tickables[i];
                if (!t.IsFinished) t.BattleTick();
            }
            tickables.RemoveAll(t => t.IsFinished);

            Services.Bullets.Tick(DeltaTime);
            CompleteEnemyDespawns();
            Services.Bullets.Flush(DeltaTime);
            Stage?.Tick(DeltaTime);
            AdvanceFrame();
        }

        public void Clear()
        {
            tickables.Clear();
            pending.Clear();
            scheduled.Clear();
            despawning.Clear();
            outgoing.Clear();
            Trees.Clear();
            SetStage(null);
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
                if (s.HasOwner && !s.Owner.IsAlive) continue;
                s.Action();
            }
        }

        void DeliverSignals()
        {
            if (outgoing.Count == 0) return;
            delivering.AddRange(outgoing);
            outgoing.Clear();
            foreach (var signal in delivering)
            {
                Deliver(signal, tickables);
                Deliver(signal, pending);
                Services.Bullets.Deliver(signal, Frame);
            }
            delivering.Clear();
        }

        void Deliver(in Signal signal, List<IBattleTickable> list)
        {
            foreach (var t in list)
            {
                if (t is EnemyAgent enemy && enemy.IsAlive && signal.Reaches(enemy)) enemy.Signals.Add(signal.Key, Frame);
            }
        }

        void CompleteEnemyDespawns()
        {
            for (var i = 0; i < despawning.Count; i++) despawning[i].CompleteDespawn();
            despawning.Clear();
        }

        internal void AdvanceFrame() => Frame++;

        void FlushPending()
        {
            if (pending.Count == 0) return;
            tickables.AddRange(pending);
            pending.Clear();
        }
    }
}
