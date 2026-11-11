using System;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.BehaviorTree
{
    [Serializable]
    public sealed class LoopNodeDefinition : DecoratorNodeDefinition
    {
        [SerializeField, Tooltip("0 = 無限")] BtInt count;

        public BtInt Count
        {
            get => count;
            set => count = value;
        }

        public override INode Build(BuildScope scope) => new LoopNode(BuildChild(scope), Mathf.Max(0, count.Resolve(scope)));
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
