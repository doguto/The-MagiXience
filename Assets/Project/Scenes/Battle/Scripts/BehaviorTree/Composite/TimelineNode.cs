using System;
using System.Collections.Generic;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.BehaviorTree
{
    [Serializable]
    public sealed class TimelineNodeDefinition : CompositeNodeDefinition
    {
        [SerializeField, Tooltip("子の並び順に対応する開始フレーム。不足分は0")]
        List<BtInt> startFrames = new();
        [SerializeField, Tooltip("0より大きければ、このフレーム数経過した時点で完了する(実行中の子は中断)。0なら全ての子の完了を待つ")]
        BtInt duration;

        public List<BtInt> StartFrames => startFrames;

        public override INode Build(BuildScope scope)
        {
            var children = BuildChildren(scope);
            var starts = new int[children.Length];
            for (var i = 0; i < starts.Length; i++) starts[i] = i < startFrames.Count ? Mathf.Max(0, startFrames[i].Resolve(scope)) : 0;
            return new TimelineNode(children, starts, duration.Resolve(scope));
        }
    }

    // 子ごとに開始フレームを指定して並列実行する。弾幕の「t=Nf で発射」を書くためのノード。
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
