using System;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.BehaviorTree
{
    [Serializable, NodeInfo("開始時に条件を 1 回判定し、成立なら 1 番目の子、不成立なら 2 番目の子を実行する (2 番目がなければ何もせず Success)")]
    public sealed class If : CompositeNode
    {
        [SerializeReference, SubclassSelector] Condition condition;

        [NonSerialized] Node chosen;

        public override string Summary => "もし " + (condition?.Summary ?? "(条件未設定)") + " → 1 / それ以外 → 2";

        protected override void OnSetup(TreeScope scope) => condition?.Setup(scope);

        protected override void OnEnter(BattleContext ctx)
        {
            condition?.Reset(ctx);
            var index = condition == null || condition.Evaluate(ctx) ? 0 : 1;
            chosen = index < Children.Count ? Children[index] : null;
        }

        protected override NodeStatus OnTick(BattleContext ctx) => chosen?.Tick(ctx) ?? NodeStatus.Success;
        protected override void OnAbort(BattleContext ctx) => chosen?.Abort(ctx);
    }
}
