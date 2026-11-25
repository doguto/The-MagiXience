using System;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.BehaviorTree
{
    [Serializable]
    public sealed class Not : Condition
    {
        [SerializeReference, SubclassSelector] Condition condition;

        public override string Summary => "NOT " + (condition?.Summary ?? "?");

        protected override void OnSetup(TreeScope scope) => condition?.Setup(scope);
        public override void Reset(BattleContext ctx) => condition?.Reset(ctx);
        public override bool Evaluate(BattleContext ctx) => condition == null || !condition.Evaluate(ctx);
    }
}
