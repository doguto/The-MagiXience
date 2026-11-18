using System;

namespace Project.Scenes.Battle.Scripts.BehaviorTree
{
    [Serializable, NodeInfo("自分を消す (撃破扱いにしない)", "Action")]
    public sealed class Despawn : ActionNode
    {
        public override string Summary => "退場";

        protected override NodeStatus OnTick(BattleContext ctx)
        {
            ctx.Self?.Despawn();
            return NodeStatus.Success;
        }
    }
}
