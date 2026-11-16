using System;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.BehaviorTree
{
    // 前フレーム以前に始まった周が完了した場合のみ同フレーム内で次の周を始める (周期をずらさないため)。
    // 同フレーム内で始まって完了した周の次は翌フレームに回す (無限ループ防止)
    [Serializable, NodeInfo("子を指定回数繰り返す。0 で無限")]
    public sealed class Loop : DecoratorNode
    {
        [SerializeField, Tooltip("0 = 無限")] BtInt count;

        [NonSerialized] int target;
        [NonSerialized] int completed;
        [NonSerialized] int iterationStartFrame;

        public BtInt Count
        {
            get => count;
            set => count = value;
        }

        public override string Summary => count.value == 0 && string.IsNullOrEmpty(count.param) ? "∞ 無限" : "× " + count;

        protected override void OnSetup(TreeScope scope) => target = Mathf.Max(0, count.Resolve(scope));

        protected override void OnEnter(BattleContext ctx)
        {
            completed = 0;
            iterationStartFrame = ctx.Frame;
        }

        protected override NodeStatus OnTick(BattleContext ctx)
        {
            while (true)
            {
                if (!Child.IsActive && iterationStartFrame < 0) iterationStartFrame = ctx.Frame;

                var status = Child.Tick(ctx);
                if (status == NodeStatus.Running) return NodeStatus.Running;
                if (status == NodeStatus.Failure) return NodeStatus.Failure;

                completed++;
                if (target > 0 && completed >= target) return NodeStatus.Success;

                var startedThisFrame = iterationStartFrame == ctx.Frame;
                iterationStartFrame = -1;
                if (startedThisFrame) return NodeStatus.Running;
            }
        }

        protected override void OnAbort(BattleContext ctx) => Child.Abort(ctx);
    }

    // 各回の開始は周期どおりに揃え、子が周期より長ければ終わったフレームに次を始める
    [Serializable, NodeInfo("子を Interval フレームの周期で繰り返す。子が早く終われば周期まで待ち、最後の回の後は待たない")]
    public sealed class Repeat : DecoratorNode
    {
        [SerializeField, Tooltip("回数。0 = 無限")] BtInt count;
        [SerializeField, Tooltip("周期: ある回の開始から次の回の開始までのフレーム数。子がこれより長ければ終わり次第すぐ次を始める")] BtInt interval = new(60);

        [NonSerialized] int target;
        [NonSerialized] int period;
        [NonSerialized] int completed;
        [NonSerialized] int elapsed;
        [NonSerialized] bool waiting;

        public BtInt Count
        {
            get => count;
            set => count = value;
        }

        public BtInt Interval
        {
            get => interval;
            set => interval = value;
        }

        public override string Summary =>
            (count.value == 0 && string.IsNullOrEmpty(count.param) ? "∞" : "× " + count) + " / 周期 " + interval.ToFrameString();

        protected override void OnSetup(TreeScope scope)
        {
            target = Mathf.Max(0, count.Resolve(scope));
            period = Mathf.Max(1, interval.Resolve(scope));
        }

        protected override void OnEnter(BattleContext ctx)
        {
            completed = 0;
            elapsed = 0;
            waiting = false;
        }

        protected override NodeStatus OnTick(BattleContext ctx)
        {
            if (waiting)
            {
                if (elapsed < period)
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

                if (elapsed < period)
                {
                    waiting = true;
                    elapsed++;
                    return NodeStatus.Running;
                }
                // period >= 1 なので、ここに来るのは前フレーム以前に始まった回だけ (同フレームで無限に回らない)
                elapsed = 0;
            }
        }

        protected override void OnAbort(BattleContext ctx) => Child.Abort(ctx);
    }
}
