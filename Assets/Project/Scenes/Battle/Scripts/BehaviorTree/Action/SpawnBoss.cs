using System;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.BehaviorTree
{
    // ---------- ボス ----------

    [Serializable, NodeInfo("ボスを出して登場の移動を流す。ツリーはまだ付けない (BossBattle で付ける)", "Action/Boss")]
    public sealed class SpawnBoss : ActionNode
    {
        [SerializeField] BossDefinition boss;

        public override string Summary => boss ? boss.name : "(ボス未設定)";

        protected override NodeStatus OnTick(BattleContext ctx)
        {
            var host = ctx.Services.BossHost;
            if (host == null || boss == null)
            {
                Debug.LogWarning("[BehaviorTree] SpawnBoss: BossHost または BossDefinition がありません");
                return NodeStatus.Failure;
            }
            return host.Spawn(boss) ? NodeStatus.Success : NodeStatus.Failure;
        }
    }
}
