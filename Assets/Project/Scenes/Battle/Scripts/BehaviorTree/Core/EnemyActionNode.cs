using System;
using Project.Scenes.Battle.Scripts.Runtime;

namespace Project.Scenes.Battle.Scripts.BehaviorTree
{
    // Self が敵であることを必要とするノード。敵のツリーにだけ置ける
    [Serializable]
    public abstract class EnemyActionNode : ActionNode
    {
        protected sealed override NodeStatus OnTick(BattleContext ctx) =>
            ctx.Self is EnemyAgent enemy ? OnTick(ctx, enemy) : NodeStatus.Failure;

        protected abstract NodeStatus OnTick(BattleContext ctx, EnemyAgent enemy);
    }
}
