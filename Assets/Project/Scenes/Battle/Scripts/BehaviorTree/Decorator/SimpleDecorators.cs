using System;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.BehaviorTree
{
    // Wait と同じ数え方: frames 目のフレームで子を始める
    [Serializable, NodeInfo("指定フレーム待ってから子を実行する")]
    public sealed class Delay : DecoratorNode
    {
        [SerializeField, Tooltip("子を開始するまでの待機フレーム数")] BtInt frames;

        [NonSerialized] int target;
        [NonSerialized] int elapsed;

        public override string Summary => "+" + frames.ToFrameString();

        protected override void OnSetup(TreeScope scope) => target = Mathf.Max(0, frames.Resolve(scope));
        protected override void OnEnter(BattleContext ctx) => elapsed = 0;

        protected override NodeStatus OnTick(BattleContext ctx)
        {
            if (elapsed < target)
            {
                elapsed++;
                return NodeStatus.Running;
            }
            return Child.Tick(ctx);
        }

        protected override void OnAbort(BattleContext ctx) => Child.Abort(ctx);
    }

    [Serializable, NodeInfo("子の Success / Failure を反転する")]
    public sealed class Inverter : DecoratorNode
    {
        protected override NodeStatus OnTick(BattleContext ctx) => Child.Tick(ctx) switch
        {
            NodeStatus.Success => NodeStatus.Failure,
            NodeStatus.Failure => NodeStatus.Success,
            _ => NodeStatus.Running,
        };

        protected override void OnAbort(BattleContext ctx) => Child.Abort(ctx);
    }

    [Serializable, NodeInfo("子の結果に関わらず Success を返す")]
    public sealed class ForceSuccess : DecoratorNode
    {
        protected override NodeStatus OnTick(BattleContext ctx) => Child.Tick(ctx) == NodeStatus.Running ? NodeStatus.Running : NodeStatus.Success;
        protected override void OnAbort(BattleContext ctx) => Child.Abort(ctx);
    }
}
