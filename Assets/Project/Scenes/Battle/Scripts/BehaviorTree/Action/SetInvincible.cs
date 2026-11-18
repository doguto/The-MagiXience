using System;
using Project.Scenes.Battle.Scripts.Runtime;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.BehaviorTree
{
    [Serializable, NodeInfo("この敵の無敵を切り替える", "Action/Enemy")]
    public sealed class SetInvincible : EnemyActionNode
    {
        [SerializeField] bool invincible = true;

        public override string Summary => invincible ? "無敵にする" : "無敵を解除";

        protected override NodeStatus OnTick(BattleContext ctx, EnemyAgent enemy)
        {
            enemy.Model?.SetInvincible(invincible);
            return NodeStatus.Success;
        }
    }
}
