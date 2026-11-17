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
}
