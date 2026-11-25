using System;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.BehaviorTree
{
    // Repeat の子を複数にしたもの。開始のたびに 1 番目の子から始める
    [Serializable, NodeInfo("子を Interval フレームの周期で 1 つずつ順に実行し、最後まで行ったら先頭に戻る。Count 回実行したら完了 (0 = 無限)。周期は小数も可")]
    public sealed class Cycle : CompositeNode
    {
        [SerializeField, Tooltip("子を実行する合計回数。0 = 無限")] BtInt count;
        [SerializeField, Tooltip("周期: ある子の開始から次の子の開始までのフレーム数。子がこれより長ければ終わり次第すぐ次を始める")] BtFloat interval = new(60);
        [SerializeField, Tooltip("空でなければ、今が何回目か (0 始まり) をこのパラメータに書き込む")] string indexParam;

        [NonSerialized] int target;
        [NonSerialized] float period;
        [NonSerialized] int completed;
        [NonSerialized] int elapsed;
        [NonSerialized] bool waiting;

        Node Current => Children[completed % Children.Count];

        public override string Summary =>
            (count.value == 0 && string.IsNullOrEmpty(count.param) ? "∞" : "× " + count) + " / 周期 " + interval.ToFrameString() + (string.IsNullOrEmpty(indexParam) ? "" : " → $" + indexParam);

        protected override void OnSetup(TreeScope scope)
        {
            target = Mathf.Max(0, count.Resolve(scope));
            period = Mathf.Max(1f, interval.Resolve(scope));
        }

        protected override void OnEnter(BattleContext ctx)
        {
            completed = 0;
            Scope?.Set(indexParam, 0);
            elapsed = 0;
            waiting = false;
        }

        protected override NodeStatus OnTick(BattleContext ctx)
        {
            if (Children.Count == 0) return NodeStatus.Success;

            if (waiting)
            {
                if (elapsed < FrameSchedule.Step(period, completed - 1))
                {
                    elapsed++;
                    return NodeStatus.Running;
                }
                waiting = false;
                elapsed = 0;
            }

            while (true)
            {
                var status = Current.Tick(ctx);
                if (status == NodeStatus.Running)
                {
                    elapsed++;
                    return NodeStatus.Running;
                }
                if (status == NodeStatus.Failure) return NodeStatus.Failure;

                completed++;
                if (target > 0 && completed >= target) return NodeStatus.Success;
                Scope?.Set(indexParam, completed);

                if (elapsed < FrameSchedule.Step(period, completed - 1))
                {
                    waiting = true;
                    elapsed++;
                    return NodeStatus.Running;
                }
                elapsed = 0;
            }
        }

        protected override void OnAbort(BattleContext ctx)
        {
            if (!waiting && Children.Count > 0) Current.Abort(ctx);
        }
    }
}
