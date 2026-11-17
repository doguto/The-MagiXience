using System;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.BehaviorTree
{
    [Serializable, NodeInfo("フェーズの区切りを通知する (進行ゲージなどが使う)", "Action/Stage")]
    public sealed class PhaseMarker : ActionNode
    {
        [SerializeField] string phaseId;

        public override string Summary => phaseId;

        protected override NodeStatus OnTick(BattleContext ctx)
        {
            ctx.Services.NotifyPhase(phaseId);
            return NodeStatus.Success;
        }
    }
}
