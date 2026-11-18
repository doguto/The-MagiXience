using System;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.BehaviorTree
{
    [Serializable, NodeInfo("A と B を 1 回計算してパラメータに書き込み、すぐ Success。計算を重ねるときは Calc を並べる")]
    public sealed class Calc : ActionNode
    {
        public enum Operator
        {
            Add,
            Subtract,
            Multiply,
            Divide,
        }

        [SerializeField, Tooltip("書き込み先のパラメータ")] string resultParam;
        [SerializeField] BtFloat a;
        [SerializeField] Operator op;
        [SerializeField] BtFloat b;

        static string Symbol(Operator o) => o switch
        {
            Operator.Add => "+",
            Operator.Subtract => "−",
            Operator.Multiply => "×",
            _ => "÷",
        };

        public override string Summary => $"${resultParam} = {a} {Symbol(op)} {b}";

        protected override NodeStatus OnTick(BattleContext ctx)
        {
            var x = a.Resolve(Scope);
            var y = b.Resolve(Scope);
            var result = op switch
            {
                Operator.Add => x + y,
                Operator.Subtract => x - y,
                Operator.Multiply => x * y,
                _ => y == 0f ? 0f : x / y,
            };
            Scope?.Set(resultParam, result);
            return NodeStatus.Success;
        }
    }
}
