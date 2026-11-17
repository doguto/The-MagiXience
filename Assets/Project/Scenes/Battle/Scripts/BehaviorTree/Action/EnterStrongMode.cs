using System;
using Project.Scenes.Battle.Scripts.Model.Entity;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.BehaviorTree
{
    // ---------- 行動 ----------

    [Serializable, NodeInfo("ボスを発狂 (Strong) 状態にする", "Action/Boss")]
    public sealed class EnterStrongMode : ActionNode
    {
        [SerializeField] HpTarget target = HpTarget.Self;

        public override string Summary => "発狂開始";

        protected override NodeStatus OnTick(BattleContext ctx)
        {
            (HpTargets.Resolve(ctx, target) as BossEntityModel)?.EnterStrongMode();
            return NodeStatus.Success;
        }
    }
}
