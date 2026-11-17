using System;
using Project.Scenes.Battle.Scripts.Model;
using Project.Scenes.Battle.Scripts.Runtime;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.BehaviorTree
{
    // 旧 BossWindCue。合図の演出 (IWindCueReceiver を持つ敵のみ) のあと風向きを切り替える
    [Serializable, NodeInfo("合図のあと風向きを切り替える", "Action/Boss")]
    public sealed class BossWindCue : ActionNode
    {
        [SerializeField] BossWindCueSettings cue = new() { enabled = true };

        public override string Summary => $"風の合図 → {cue.direction} ({cue.cueSeconds:0.##}s)";

        protected override NodeStatus OnTick(BattleContext ctx)
        {
            var wind = ctx.Stage?.Get<WindService>();
            if (wind == null)
            {
                ctx.Stage?.WarnMissing<WindService>(nameof(BossWindCue));
                return NodeStatus.Success;
            }
            wind.Apply(cue.CreateWind(wind.Direction));
            if (ctx.Self != null && ctx.Self.TryGetComponent<IWindCueReceiver>(out var receiver)) receiver.PlayCue(cue);
            return NodeStatus.Success;
        }
    }
}
