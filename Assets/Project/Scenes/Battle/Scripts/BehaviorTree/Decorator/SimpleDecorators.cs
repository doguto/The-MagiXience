using System;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.BehaviorTree
{
    [Serializable, NodeInfo("指定フレーム待ってから子を実行する")]
    public sealed class DelayNodeDefinition : DecoratorNodeDefinition
    {
        [SerializeField, Tooltip("子を開始するまでの待機フレーム数")] BtInt frames;

        public override string Summary => "+" + frames.ToFrameString();

        public override INode Build(BuildScope scope) =>
            new SequenceNode(new[] { new WaitNode(Mathf.Max(0, frames.Resolve(scope))), BuildChild(scope) });
    }

    [Serializable, NodeInfo("子の Success / Failure を反転する")]
    public sealed class InverterNodeDefinition : DecoratorNodeDefinition
    {
        public override INode Build(BuildScope scope) => new MapResultNode(BuildChild(scope), s => s == NodeStatus.Success ? NodeStatus.Failure : NodeStatus.Success);
    }

    [Serializable, NodeInfo("子の結果に関わらず Success を返す")]
    public sealed class ForceSuccessNodeDefinition : DecoratorNodeDefinition
    {
        public override INode Build(BuildScope scope) => new MapResultNode(BuildChild(scope), _ => NodeStatus.Success);
    }

    public sealed class MapResultNode : Node
    {
        readonly INode child;
        readonly Func<NodeStatus, NodeStatus> map;

        public MapResultNode(INode child, Func<NodeStatus, NodeStatus> map)
        {
            this.child = child;
            this.map = map;
        }

        protected override NodeStatus OnTick(BattleContext ctx)
        {
            var status = child.Tick(ctx);
            return status == NodeStatus.Running ? status : map(status);
        }

        protected override void OnAbort(BattleContext ctx) => child.Abort(ctx);
    }
}
