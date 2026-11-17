using System;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.BehaviorTree
{
    [Serializable, NodeInfo("条件が成立するまで待機する")]
    public sealed class WaitUntil : ActionNode
    {
        [SerializeReference, SubclassSelector] Condition condition;

        public override string Summary => "待機: " + (condition?.Summary ?? "(未設定)");

        protected override void OnSetup(TreeScope scope) => condition?.Setup(scope);
        protected override void OnEnter(BattleContext ctx) => condition?.Reset(ctx);
        protected override NodeStatus OnTick(BattleContext ctx) => condition == null || condition.Evaluate(ctx) ? NodeStatus.Success : NodeStatus.Running;
    }
}
