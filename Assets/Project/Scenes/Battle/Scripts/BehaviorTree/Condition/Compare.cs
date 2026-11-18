using System;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.BehaviorTree
{
    public enum CompareOperator
    {
        GreaterOrEqual,
        Greater,
        LessOrEqual,
        Less,
        Equal,
        NotEqual,
    }

    // パラメータは判定のたびに読み直す (HpSections や Repeat が実行中に書き込む値を見るため)
    [Serializable]
    public sealed class Compare : Condition
    {
        [SerializeField] BtFloat a;
        [SerializeField] CompareOperator op;
        [SerializeField] BtFloat b;

        [NonSerialized] TreeScope scope;

        static string Symbol(CompareOperator o) => o switch
        {
            CompareOperator.GreaterOrEqual => "≧",
            CompareOperator.Greater => ">",
            CompareOperator.LessOrEqual => "≦",
            CompareOperator.Less => "<",
            CompareOperator.Equal => "=",
            _ => "≠",
        };

        public override string Summary => $"{a} {Symbol(op)} {b}";

        protected override void OnSetup(TreeScope treeScope) => scope = treeScope;

        public override bool Evaluate(BattleContext ctx)
        {
            var x = a.Resolve(scope);
            var y = b.Resolve(scope);
            return op switch
            {
                CompareOperator.GreaterOrEqual => x >= y,
                CompareOperator.Greater => x > y,
                CompareOperator.LessOrEqual => x <= y,
                CompareOperator.Less => x < y,
                CompareOperator.Equal => Mathf.Approximately(x, y),
                _ => !Mathf.Approximately(x, y),
            };
        }
    }
}
