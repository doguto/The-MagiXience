using System;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.BehaviorTree
{
    // 開始フレーム f から数えて f + frames のフレームで Success。0 なら即 Success
    [Serializable, NodeInfo("指定フレーム待機する (60f = 1秒)")]
    public sealed class Wait : ActionNode
    {
        [SerializeField, Tooltip("待機フレーム数(60 = 1秒)")] BtInt frames;

        [NonSerialized] int target;
        [NonSerialized] int elapsed;

        public BtInt Frames
        {
            get => frames;
            set => frames = value;
        }

        public override string Summary => frames.ToFrameString();

        protected override void OnSetup(TreeScope scope) => target = Mathf.Max(0, frames.Resolve(scope));
        protected override void OnEnter(BattleContext ctx) => elapsed = 0;

        protected override NodeStatus OnTick(BattleContext ctx)
        {
            if (elapsed >= target) return NodeStatus.Success;
            elapsed++;
            return NodeStatus.Running;
        }
    }
}
