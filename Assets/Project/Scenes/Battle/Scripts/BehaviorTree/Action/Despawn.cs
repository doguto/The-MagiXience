using System;
using Project.Scenes.Battle.Scripts.Runtime;

namespace Project.Scenes.Battle.Scripts.BehaviorTree
{
    [Serializable, NodeInfo("この敵を消す (撃破扱いにしない)", "Action/Actor")]
    public sealed class Despawn : ActionNode
    {
        public override string Summary => "退場";

        protected override NodeStatus OnTick(BattleContext ctx)
        {
            if (ctx.Self != null)
            {
                ctx.World.CancelScheduled(ctx.Self);
                ActorDespawn.Despawn(ctx.Self.gameObject);
            }
            return NodeStatus.Success;
        }
    }
}
