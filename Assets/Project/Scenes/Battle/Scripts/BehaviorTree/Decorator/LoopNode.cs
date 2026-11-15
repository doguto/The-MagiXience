using System;
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

    [Serializable, NodeInfo("子を Interval フレームの周期で繰り返す。子が早く終われば周期まで待ち、最後の回の後は待たない")]
    public sealed class RepeatNodeDefinition : DecoratorNodeDefinition
    {
        [SerializeField, Tooltip("回数。0 = 無限")] BtInt count;
        [SerializeField, Tooltip("周期: ある回の開始から次の回の開始までのフレーム数。子がこれより長ければ終わり次第すぐ次を始める")] BtInt interval = new(60);

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
            new RepeatNode(BuildChild(scope), Mathf.Max(0, count.Resolve(scope)), Mathf.Max(1, interval.Resolve(scope)));

        public override string Summary =>
            (count.value == 0 && string.IsNullOrEmpty(count.param) ? "∞" : "× " + count) + " / 周期 " + interval.ToFrameString();
    }

    // 子を直列に繰り返す。各回の開始は周期どおりに揃え、子が周期より長ければ終わったフレームに次を始める
    public sealed class RepeatNode : Node
    {
        readonly INode child;
        readonly int count;
        readonly int period;
        int completed;
        int elapsed;
        bool waiting;

        public RepeatNode(INode child, int count, int period)
        {
            this.child = child;
            this.count = count;
            this.period = period;
        }

        protected override void OnEnter(BattleContext ctx)
        {
            completed = 0;
            elapsed = 0;
            waiting = false;
        }

        protected override NodeStatus OnTick(BattleContext ctx)
        {
            if (waiting)
            {
                if (elapsed < period)
                {
                    elapsed++;
                    return NodeStatus.Running;
                }
                waiting = false;
                elapsed = 0;
            }

            while (true)
            {
                var status = child.Tick(ctx);
                if (status == NodeStatus.Running)
                {
                    elapsed++;
                    return NodeStatus.Running;
                }
                if (status == NodeStatus.Failure) return NodeStatus.Failure;

                completed++;
                if (count > 0 && completed >= count) return NodeStatus.Success;

                if (elapsed < period)
                {
                    waiting = true;
                    elapsed++;
                    return NodeStatus.Running;
                }
                // period >= 1 なので、ここに来るのは前フレーム以前に始まった回だけ (同フレームで無限に回らない)
                elapsed = 0;
            }
        }

        protected override void OnAbort(BattleContext ctx) => child.Abort(ctx);
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
