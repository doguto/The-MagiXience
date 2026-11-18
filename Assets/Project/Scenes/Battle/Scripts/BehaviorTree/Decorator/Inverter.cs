using System;

namespace Project.Scenes.Battle.Scripts.BehaviorTree
{
    [Serializable, NodeInfo("子の Success / Failure を反転する")]
    public sealed class Inverter : DecoratorNode
    {
        protected override NodeStatus OnTick(BattleContext ctx) => Child.Tick(ctx) switch
        {
            NodeStatus.Success => NodeStatus.Failure,
            NodeStatus.Failure => NodeStatus.Success,
            _ => NodeStatus.Running,
        };

        protected override void OnAbort(BattleContext ctx) => Child.Abort(ctx);
    }
}
