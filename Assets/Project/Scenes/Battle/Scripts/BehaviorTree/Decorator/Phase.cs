using System;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.BehaviorTree
{
    [Serializable, NodeInfo("フェーズ 1 つ分。開始を通知し、終了条件が成立するまで子を実行する (Until と同じ)。実行中は終了条件の進み具合を進行ゲージへ流す")]
    public sealed class Phase : Until
    {
        [SerializeField] string phaseId;

        public string PhaseId => phaseId;

        public override string Summary => $"«{phaseId}» 〜 {base.Summary}";

        protected override void OnEnter(BattleContext ctx)
        {
            base.OnEnter(ctx);
            ctx.Services.NotifyPhase(phaseId);
        }

        protected override NodeStatus OnTick(BattleContext ctx)
        {
            ctx.Services.SetPhaseProgress(Condition?.Progress(ctx));
            return base.OnTick(ctx);
        }

        protected override void OnExit(BattleContext ctx) => ctx.Services.SetPhaseProgress(null);
    }
}
