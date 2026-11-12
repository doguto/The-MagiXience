using System;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.BehaviorTree
{
    public enum ParallelPolicy
    {
        [Tooltip("全ての子が完了したら完了")] All,
        [Tooltip("どれか1つの子が完了したら、残りを中断して完了")] Any,
        [Tooltip("Duration のフレーム数が経過したら、実行中の子を中断して完了。子が先に終わっても Duration まで待つ")] Duration,
    }

    [Serializable, NodeInfo("子を同時に実行する。終了は Policy で選択。子の開始時刻は Delay で指定")]
    public sealed class ParallelNodeDefinition : CompositeNodeDefinition
    {
        [SerializeField, Tooltip("All: 全て完了で終了 / Any: どれか1つ完了で終了 / Duration: 指定フレーム経過で終了")]
        ParallelPolicy policy = ParallelPolicy.All;

        [SerializeField, ShowWhen(nameof(policy), ParallelPolicy.Duration), Tooltip("Policy が Duration のときの長さ (フレーム)")]
        BtInt duration;

        public ParallelPolicy Policy
        {
            get => policy;
            set => policy = value;
        }

        public BtInt Duration
        {
            get => duration;
            set => duration = value;
        }

        public override INode Build(BuildScope scope)
        {
            var frames = policy == ParallelPolicy.Duration ? Mathf.Max(0, duration.Resolve(scope)) : 0;
            return new ParallelNode(BuildChildren(scope), policy, frames);
        }

        public override string Summary => policy switch
        {
            ParallelPolicy.All => "全て完了で終了",
            ParallelPolicy.Any => "どれか1つ完了で終了",
            _ => "長さ " + duration.ToFrameString(),
        };
    }

    // Duration 指定時は攻撃パターンの 1 周期を表せるため、移動と攻撃の周期を同じ長さで揃えられる。
    public sealed class ParallelNode : Node
    {
        readonly INode[] children;
        readonly ParallelPolicy policy;
        readonly int duration;
        readonly bool[] finished;
        int elapsed;

        public ParallelNode(INode[] children, ParallelPolicy policy, int duration = 0)
        {
            this.children = children;
            this.policy = policy;
            this.duration = policy == ParallelPolicy.Duration ? duration : 0;
            finished = new bool[children.Length];
        }

        protected override void OnEnter(BattleContext ctx)
        {
            elapsed = 0;
            Array.Clear(finished, 0, finished.Length);
        }

        protected override NodeStatus OnTick(BattleContext ctx)
        {
            if (duration > 0 && elapsed >= duration)
            {
                AbortRunning(ctx);
                return NodeStatus.Success;
            }

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

                if (policy == ParallelPolicy.Any)
                {
                    AbortRunning(ctx);
                    return NodeStatus.Success;
                }
            }

            elapsed++;
            if (duration > 0) return NodeStatus.Running;
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
