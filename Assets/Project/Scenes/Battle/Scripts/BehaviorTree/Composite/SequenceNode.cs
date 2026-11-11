using System;

namespace Project.Scenes.Battle.Scripts.BehaviorTree
{
    [Serializable]
    public sealed class SequenceNodeDefinition : CompositeNodeDefinition
    {
        public override INode Build(BuildScope scope) => new SequenceNode(BuildChildren(scope));
    }

    // 子を順に実行する。子が完了したフレームのうちに次の子へ進む。
    public sealed class SequenceNode : Node
    {
        readonly INode[] children;
        int index;

        public SequenceNode(INode[] children) => this.children = children;

        protected override void OnEnter(BattleContext ctx) => index = 0;

        protected override NodeStatus OnTick(BattleContext ctx)
        {
            while (index < children.Length)
            {
                var status = children[index].Tick(ctx);
                if (status != NodeStatus.Success) return status;
                index++;
            }
            return NodeStatus.Success;
        }

        protected override void OnAbort(BattleContext ctx)
        {
            if (index < children.Length) children[index].Abort(ctx);
        }
    }

    [Serializable]
    public sealed class SelectorNodeDefinition : CompositeNodeDefinition
    {
        public override INode Build(BuildScope scope) => new SelectorNode(BuildChildren(scope));
    }

    // 子を順に試し、最初に Success した時点で Success。
    public sealed class SelectorNode : Node
    {
        readonly INode[] children;
        int index;

        public SelectorNode(INode[] children) => this.children = children;

        protected override void OnEnter(BattleContext ctx) => index = 0;

        protected override NodeStatus OnTick(BattleContext ctx)
        {
            while (index < children.Length)
            {
                var status = children[index].Tick(ctx);
                if (status != NodeStatus.Failure) return status;
                index++;
            }
            return NodeStatus.Failure;
        }

        protected override void OnAbort(BattleContext ctx)
        {
            if (index < children.Length) children[index].Abort(ctx);
        }
    }
}
