using System;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.BehaviorTree
{
    [Serializable, NodeInfo("出ているボスにツリーを付けて戦闘を始め、撃破演出が終わるまで待つ", "Action/Boss")]
    public sealed class BossBattle : ActionNode
    {
        [SerializeField, Tooltip("未指定なら BossDefinition のツリー")] BehaviorTreeAsset tree;

        [NonSerialized] bool started;

        public BehaviorTreeAsset Tree => tree;

        public override string Summary => tree ? tree.name : "(BossDefinition のツリー)";

        protected override void OnEnter(BattleContext ctx)
        {
            var host = ctx.Services.BossHost;
            var resolved = tree != null ? tree : host?.Current != null ? host.Current.Tree : null;
            started = host != null && host.Current != null && host.StartBattle(resolved) != null;
            if (!started) Debug.LogWarning("[BehaviorTree] BossBattle: ボスが出ていないか、ツリーがありません");
        }

        protected override NodeStatus OnTick(BattleContext ctx)
        {
            if (!started) return NodeStatus.Failure;
            return ctx.Services.BossHost.IsDefeated ? NodeStatus.Success : NodeStatus.Running;
        }
    }
}
