using System;
using System.Collections.Generic;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.BehaviorTree
{
    [Serializable]
    public sealed class TimelineNodeDefinition : CompositeNodeDefinition
    {
        [SerializeField, Tooltip("0より大きければ、このフレーム数経過した時点で完了する(実行中の子は中断)。0なら全ての子の完了を待つ")]
        BtInt duration;

        public BtInt Duration
        {
            get => duration;
            set => duration = value;
        }

        public override INode Build(BuildScope scope)
        {
            var children = BuildChildren(scope);
            return new TimelineNode(children, new int[children.Length], Mathf.Max(0, duration.Resolve(scope)));
        }

        public override string Summary => duration.value == 0 && string.IsNullOrEmpty(duration.param) ? "全ての子の完了まで" : "長さ " + duration.ToFrameString();
    }

    // 子を並列実行し、duration 指定時はその長さで打ち切る。子の開始時刻は Delay デコレータで指定する。
    // 攻撃パターンの 1 周期をこのノードで表すと、移動と攻撃の周期を同じ duration で揃えられる。
    public sealed class TimelineNode : Node
    {
        readonly INode[] children;
        readonly int[] startFrames;
        readonly int duration;
        readonly bool[] finished;
        int elapsed;

        public TimelineNode(INode[] children, int[] startFrames, int duration)
        {
            this.children = children;
            this.startFrames = startFrames;
            this.duration = duration;
            finished = new bool[children.Length];
        }

        protected override void OnEnter(BattleContext ctx)
        {
            elapsed = 0;
            Array.Clear(finished, 0, finished.Length);
        }

        protected override NodeStatus OnTick(BattleContext ctx)
        {
            if (duration > 0 && elapsed >= duration)
            {
                AbortRunning(ctx);
                return NodeStatus.Success;
            }

            var allDone = true;
            for (var i = 0; i < children.Length; i++)
            {
                if (finished[i]) continue;
                if (elapsed < startFrames[i])
                {
                    allDone = false;
                    continue;
                }

                var status = children[i].Tick(ctx);
                if (status == NodeStatus.Running)
                {
                    allDone = false;
                    continue;
                }

                finished[i] = true;
                if (status == NodeStatus.Failure)
                {
                    AbortRunning(ctx);
                    return NodeStatus.Failure;
                }
            }

            elapsed++;
            if (duration > 0) return NodeStatus.Running;
            return allDone ? NodeStatus.Success : NodeStatus.Running;
        }

        protected override void OnAbort(BattleContext ctx) => AbortRunning(ctx);

        void AbortRunning(BattleContext ctx)
        {
            for (var i = 0; i < children.Length; i++)
            {
                if (!finished[i]) children[i].Abort(ctx);
            }
        }
    }
}
