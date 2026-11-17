using System;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.BehaviorTree
{
    [Serializable, NodeInfo("この敵の無敵を切り替える", "Action/Actor")]
    public sealed class SetInvincible : ActionNode
    {
        [SerializeField] bool invincible = true;

        public override string Summary => invincible ? "無敵にする" : "無敵を解除";

        protected override NodeStatus OnTick(BattleContext ctx)
        {
            ctx.Self?.Model?.SetInvincible(invincible);
            return NodeStatus.Success;
        }
    }
}
