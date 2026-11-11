using System;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.BehaviorTree
{
    public enum UntilChildCompletion
    {
        [Tooltip("子の結果をそのまま返して終了")] Finish,
        [Tooltip("子をやり直し、条件成立まで続ける")] Restart,
        [Tooltip("子の完了後は何もせず条件成立を待つ")] Hold,
    }

    [Serializable, NodeInfo("条件が成立した瞬間に子を中断して完了する。毎フレーム子より先に判定")]
    public sealed class UntilNodeDefinition : DecoratorNodeDefinition
    {
        [SerializeReference, SubclassSelector] ConditionDefinition condition;
        [SerializeField] UntilChildCompletion onChildCompleted = UntilChildCompletion.Finish;

        public ConditionDefinition Condition
        {
            get => condition;
            set => condition = value;
        }

        public UntilChildCompletion OnChildCompleted
        {
            get => onChildCompleted;
            set => onChildCompleted = value;
        }

        public override string Summary => (condition?.Summary ?? "(条件未設定)") + (onChildCompleted == UntilChildCompletion.Finish ? "" : $"  [{onChildCompleted}]");

        public override INode Build(BuildScope scope)
        {
            var cond = condition?.Build(scope) ?? new ConstantCondition(false);
            return new UntilNode(BuildChild(scope), cond, onChildCompleted);
        }
    }

    // 毎フレーム子より先に条件を評価し、成立したら子を中断して Success。
    public sealed class UntilNode : Node
    {
        readonly INode child;
        readonly ICondition condition;
        readonly UntilChildCompletion onChildCompleted;
        bool holding;

        public UntilNode(INode child, ICondition condition, UntilChildCompletion onChildCompleted)
        {
            this.child = child;
            this.condition = condition;
            this.onChildCompleted = onChildCompleted;
        }

        protected override void OnEnter(BattleContext ctx)
        {
            holding = false;
            condition.Reset(ctx);
        }

        protected override NodeStatus OnTick(BattleContext ctx)
        {
            if (condition.Evaluate(ctx))
            {
                child.Abort(ctx);
                return NodeStatus.Success;
            }

            if (holding) return NodeStatus.Running;

            var status = child.Tick(ctx);
            if (status == NodeStatus.Running) return NodeStatus.Running;

            switch (onChildCompleted)
            {
                case UntilChildCompletion.Restart:
                    return NodeStatus.Running;
                case UntilChildCompletion.Hold:
                    holding = true;
                    return NodeStatus.Running;
                default:
                    return status;
            }
        }

        protected override void OnAbort(BattleContext ctx) => child.Abort(ctx);
    }
}
