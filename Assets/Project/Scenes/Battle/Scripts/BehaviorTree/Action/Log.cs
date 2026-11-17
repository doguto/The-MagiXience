using System;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.BehaviorTree
{
    [Serializable, NodeInfo("コンソールにログを出力する (デバッグ用)")]
    public sealed class Log : ActionNode
    {
        [SerializeField] string message;

        public override string Summary => message;

        protected override NodeStatus OnTick(BattleContext ctx)
        {
            Debug.Log($"[BehaviorTree] frame={ctx.Frame} {message}");
            return NodeStatus.Success;
        }
    }
}
