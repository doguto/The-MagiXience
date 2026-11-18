using System;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.BehaviorTree
{
    // 各回の開始は周期どおりに揃え、子が周期より長ければ終わったフレームに次を始める
    [Serializable, NodeInfo("子を Interval フレームの周期で繰り返す。子が早く終われば周期まで待ち、最後の回の後は待たない。周期は小数も可 (22.5 なら 22, 23, 23, 22, …)")]
    public sealed class Repeat : DecoratorNode
    {
        [SerializeField, Tooltip("回数。0 = 無限")] BtInt count;
        [SerializeField, Tooltip("周期: ある回の開始から次の回の開始までのフレーム数。子がこれより長ければ終わり次第すぐ次を始める")] BtFloat interval = new(60);
        [SerializeField, Tooltip("空でなければ、今が何回目か (0 始まり) をこのパラメータに書き込む")] string indexParam;

        [NonSerialized] int target;
        [NonSerialized] float period;
        [NonSerialized] int completed;
        [NonSerialized] int elapsed;
        [NonSerialized] bool waiting;

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
                var status = Child.Tick(ctx);
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
                // 間隔は 1 以上なので、ここに来るのは前フレーム以前に始まった回だけ (同フレームで無限に回らない)
                elapsed = 0;
            }
        }

        protected override void OnAbort(BattleContext ctx) => Child.Abort(ctx);
    }
}
