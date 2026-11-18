using System;
using Project.Scripts.Extensions;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.BehaviorTree
{
    [Serializable, NodeInfo("効果音を鳴らす", "Action")]
    public sealed class PlaySe : ActionNode
    {
        [SerializeField] SeType se;

        public override string Summary => se.ToString();

        protected override NodeStatus OnTick(BattleContext ctx)
        {
            Emitter.PlaySe(ctx, se);
            return NodeStatus.Success;
        }
    }
}
