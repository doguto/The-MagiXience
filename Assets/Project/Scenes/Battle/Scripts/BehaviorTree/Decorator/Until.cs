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
    public class Until : DecoratorNode
    {
        [SerializeReference, SubclassSelector] Condition condition;
        [SerializeField] UntilChildCompletion onChildCompleted = UntilChildCompletion.Finish;

        [NonSerialized] bool holding;

        public Condition Condition
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

        protected override void OnSetup(TreeScope scope) => condition?.Setup(scope);

        protected override void OnEnter(BattleContext ctx)
        {
            holding = false;
            condition?.Reset(ctx);
        }

        protected override NodeStatus OnTick(BattleContext ctx)
        {
            if (condition != null && condition.Evaluate(ctx))
            {
                Child.Abort(ctx);
                return NodeStatus.Success;
            }

            if (holding) return NodeStatus.Running;

            var status = Child.Tick(ctx);
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

        protected override void OnAbort(BattleContext ctx) => Child.Abort(ctx);

        public override void FastForward(BattleContext ctx)
        {
            condition?.FastForward(ctx);
            base.FastForward(ctx);
        }
    }
}
