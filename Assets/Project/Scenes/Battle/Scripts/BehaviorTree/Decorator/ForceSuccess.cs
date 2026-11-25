using System;

namespace Project.Scenes.Battle.Scripts.BehaviorTree
{
    [Serializable, NodeInfo("子の結果に関わらず Success を返す")]
    public sealed class ForceSuccess : DecoratorNode
    {
        protected override NodeStatus OnTick(BattleContext ctx) => Child.Tick(ctx) == NodeStatus.Running ? NodeStatus.Running : NodeStatus.Success;
        protected override void OnAbort(BattleContext ctx) => Child.Abort(ctx);
    }
}
