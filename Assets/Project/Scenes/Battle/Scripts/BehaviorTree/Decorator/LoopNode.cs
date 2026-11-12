using System;
using System.Collections.Generic;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.BehaviorTree
{
    [Serializable, NodeInfo("子を指定回数繰り返す。0 で無限")]
    public sealed class LoopNodeDefinition : DecoratorNodeDefinition
    {
        [SerializeField, Tooltip("0 = 無限")] BtInt count;

        public BtInt Count
        {
            get => count;
            set => count = value;
        }

        public override INode Build(BuildScope scope) => new LoopNode(BuildChild(scope), Mathf.Max(0, count.Resolve(scope)));

        public override string Summary => count.value == 0 && string.IsNullOrEmpty(count.param) ? "∞ 無限" : "× " + count;
    }

    [Serializable, NodeInfo("子を Interval フレームおきに開始する。前の回が終わっていなくても次を始める (重なってよい)")]
    public sealed class RepeatNodeDefinition : DecoratorNodeDefinition
    {
        [SerializeField, Tooltip("開始する回数。0 = 無限")] BtInt count;
        [SerializeField, Tooltip("開始の間隔 (フレーム)")] BtInt interval = new(60);

        public BtInt Count
        {
            get => count;
            set => count = value;
        }

        public BtInt Interval
        {
            get => interval;
            set => interval = value;
        }

        public override INode Build(BuildScope scope) =>
            new RepeatNode(() => BuildChild(scope), Mathf.Max(0, count.Resolve(scope)), Mathf.Max(1, interval.Resolve(scope)));

        public override string Summary =>
            (count.value == 0 && string.IsNullOrEmpty(count.param) ? "∞" : "× " + count) + " / " + interval.ToFrameString() + " おき";
    }

    // 子を一定間隔で開始し、実行中の回はすべて並行して進める。指定回数を開始し、全て終わったら Success
    public sealed class RepeatNode : Node
    {
        readonly Func<INode> factory;
        readonly int count;
        readonly int interval;
        readonly List<INode> running = new();
        readonly Stack<INode> idle = new();
        int started;
        int elapsed;

        public RepeatNode(Func<INode> factory, int count, int interval)
        {
            this.factory = factory;
            this.count = count;
            this.interval = interval;
        }

        protected override void OnEnter(BattleContext ctx)
        {
            started = 0;
            elapsed = 0;
        }

        protected override NodeStatus OnTick(BattleContext ctx)
        {
            if ((count == 0 || started < count) && elapsed == started * interval)
            {
                running.Add(idle.Count > 0 ? idle.Pop() : factory());
                started++;
            }

            for (var i = 0; i < running.Count; i++)
            {
                var status = running[i].Tick(ctx);
                if (status == NodeStatus.Running) continue;
                if (status == NodeStatus.Failure)
                {
                    AbortRunning(ctx);
                    return NodeStatus.Failure;
                }
                idle.Push(running[i]);
                running.RemoveAt(i--);
            }

            elapsed++;
            return count > 0 && started >= count && running.Count == 0 ? NodeStatus.Success : NodeStatus.Running;
        }

        protected override void OnAbort(BattleContext ctx) => AbortRunning(ctx);

        void AbortRunning(BattleContext ctx)
        {
            foreach (var node in running)
            {
                node.Abort(ctx);
                idle.Push(node);
            }
            running.Clear();
        }
    }

    // 子を指定回数繰り返す。子が Failure を返したら Failure。
    // 前フレーム以前に始まった周が完了した場合のみ同フレーム内で次の周を始める(周期をずらさないため)。
    // 同フレーム内で始まって完了した周の次は翌フレームに回す(無限ループ防止)。
    public sealed class LoopNode : Node
    {
        readonly INode child;
        readonly int count;
        int completed;
        int iterationStartFrame;

        public LoopNode(INode child, int count)
        {
            this.child = child;
            this.count = count;
        }

        protected override void OnEnter(BattleContext ctx)
        {
            completed = 0;
            iterationStartFrame = ctx.Frame;
        }

        protected override NodeStatus OnTick(BattleContext ctx)
        {
            while (true)
            {
                if (!child.IsActive && iterationStartFrame < 0) iterationStartFrame = ctx.Frame;

                var status = child.Tick(ctx);
                if (status == NodeStatus.Running) return NodeStatus.Running;
                if (status == NodeStatus.Failure) return NodeStatus.Failure;

                completed++;
                if (count > 0 && completed >= count) return NodeStatus.Success;

                var startedThisFrame = iterationStartFrame == ctx.Frame;
                iterationStartFrame = -1;
                if (startedThisFrame) return NodeStatus.Running;
            }
        }

        protected override void OnAbort(BattleContext ctx) => child.Abort(ctx);
    }
}
