using System;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.BehaviorTree
{
    public enum ParallelPolicy
    {
        RequireAll,
        RequireOne,
    }

    [Serializable]
    public sealed class ParallelNodeDefinition : CompositeNodeDefinition
    {
        [SerializeField, Tooltip("RequireAll: 全て成功で完了 / RequireOne: どれか1つ成功で残りを中断して完了")]
        ParallelPolicy policy = ParallelPolicy.RequireAll;

        public ParallelPolicy Policy
        {
            get => policy;
            set => policy = value;
        }

        public override INode Build(BuildScope scope) => new ParallelNode(BuildChildren(scope), policy);

        public override string Summary => policy == ParallelPolicy.RequireAll ? "全て完了で終了" : "どれか1つ完了で終了";
    }

    public sealed class ParallelNode : Node
    {
        readonly INode[] children;
        readonly ParallelPolicy policy;
        readonly bool[] finished;

        public ParallelNode(INode[] children, ParallelPolicy policy)
        {
            this.children = children;
            this.policy = policy;
            finished = new bool[children.Length];
        }

        protected override void OnEnter(BattleContext ctx) => Array.Clear(finished, 0, finished.Length);

        protected override NodeStatus OnTick(BattleContext ctx)
        {
            var allDone = true;
            for (var i = 0; i < children.Length; i++)
            {
                if (finished[i]) continue;

                var status = children[i].Tick(ctx);
                if (status == NodeStatus.Running)
                {
                    allDone = false;
                    continue;
                }

                finished[i] = true;
                if (status == NodeStatus.Failure)
                {
                    AbortRunning(ctx);
                    return NodeStatus.Failure;
                }

                if (policy == ParallelPolicy.RequireOne)
                {
                    AbortRunning(ctx);
                    return NodeStatus.Success;
                }
            }

            return allDone ? NodeStatus.Success : NodeStatus.Running;
        }

        protected override void OnAbort(BattleContext ctx) => AbortRunning(ctx);

        void AbortRunning(BattleContext ctx)
        {
            for (var i = 0; i < children.Length; i++)
            {
                if (!finished[i]) children[i].Abort(ctx);
            }
        }
    }
}
