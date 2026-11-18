using System;
using Project.Scenes.Battle.Scripts.Runtime;
using Project.Scenes.Battle.Scripts.Runtime.Motion;

namespace Project.Scenes.Battle.Scripts.BehaviorTree
{
    [Serializable, NodeInfo("出現させた側から渡された移動を実行する。移動が終わると完了 (無限の移動は完了しない)", "Action/Move")]
    public sealed class MoveSelf : ActionNode
    {
        [NonSerialized] MotionSequence sequence;

        public override string Summary => "渡された移動";

        protected override void OnEnter(BattleContext ctx)
        {
            var self = ctx.Self;
            sequence = self?.Motion != null && self.Motion.Count > 0
                ? new MotionSequence(self.Motion, self.Body, new MotionLaunch { Direction = self.Direction, AttackTime = self.AttackTime })
                : null;
        }

        protected override NodeStatus OnTick(BattleContext ctx)
        {
            if (sequence == null) return NodeStatus.Success;
            sequence.Tick(BattleWorld.DeltaTime);
            // 開始できない設定の移動 (頂点が発射位置と同じ放物線など) は、その場で消す
            if (sequence.Rejected)
            {
                ctx.Self.Despawn();
                return NodeStatus.Failure;
            }
            return sequence.IsFinished ? NodeStatus.Success : NodeStatus.Running;
        }

        protected override void OnExit(BattleContext ctx)
        {
            sequence?.Stop();
            sequence = null;
        }
    }
}
