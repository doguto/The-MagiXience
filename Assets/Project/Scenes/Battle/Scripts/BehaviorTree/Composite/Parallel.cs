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

    // Duration 指定時は攻撃パターンの 1 周期を表せるため、移動と攻撃の周期を同じ長さで揃えられる
    [Serializable, NodeInfo("子を同時に実行する。終了は Policy で選択。子の開始時刻は Delay で指定")]
    public sealed class Parallel : CompositeNode
    {
        [SerializeField, Tooltip("All: 全て完了で終了 / Any: どれか1つ完了で終了 / Duration: 指定フレーム経過で終了")]
        ParallelPolicy policy = ParallelPolicy.All;

        [SerializeField, ShowWhen(nameof(policy), ParallelPolicy.Duration), Tooltip("Policy が Duration のときの長さ (フレーム)")]
        BtInt duration;

        [NonSerialized] int frames;
        [NonSerialized] bool[] finished;
        [NonSerialized] int elapsed;

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

        public override string Summary => policy switch
        {
            ParallelPolicy.All => "全て完了で終了",
            ParallelPolicy.Any => "どれか1つ完了で終了",
            _ => "長さ " + duration.ToFrameString(),
        };

        protected override void OnSetup(TreeScope scope)
        {
            frames = policy == ParallelPolicy.Duration ? Mathf.Max(0, duration.Resolve(scope)) : 0;
            finished = new bool[Children.Count];
        }

        protected override void OnEnter(BattleContext ctx)
        {
            elapsed = 0;
            Array.Clear(finished, 0, finished.Length);
        }

        protected override NodeStatus OnTick(BattleContext ctx)
        {
            if (frames > 0 && elapsed >= frames)
            {
                AbortRunning(ctx);
                return NodeStatus.Success;
            }

            var allDone = true;
            for (var i = 0; i < Children.Count; i++)
            {
                if (finished[i]) continue;

                var status = Children[i].Tick(ctx);
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
            if (frames > 0) return NodeStatus.Running;
            return allDone ? NodeStatus.Success : NodeStatus.Running;
        }

        protected override void OnAbort(BattleContext ctx) => AbortRunning(ctx);

        void AbortRunning(BattleContext ctx)
        {
            for (var i = 0; i < Children.Count; i++)
            {
                if (!finished[i]) Children[i].Abort(ctx);
            }
        }
    }
}
