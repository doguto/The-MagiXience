using System;
using Project.Scripts.Extensions;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.BehaviorTree
{
    [Serializable, NodeInfo("この敵が倒されたときに 1 回だけ実行するツリーを設定する", "Action/Actor")]
    public sealed class SetDeathBehavior : ActionNode
    {
        [SerializeField] BehaviorTreeAsset tree;
        [SerializeField, Tooltip("ツリーの Object パラメータ名。指定するとその値のツリーを使う (値が空なら撃破時の行動なし)")] string treeParam;

        [NonSerialized] BehaviorTreeAsset resolved;

        public override string Summary =>
            !string.IsNullOrEmpty(treeParam) ? $"撃破時: ${treeParam}" : tree ? $"撃破時: {tree.name}" : "撃破時の行動なし";

        protected override void OnSetup(TreeScope scope) => resolved = ParamObjects.Resolve(scope, treeParam, tree);

        protected override NodeStatus OnTick(BattleContext ctx)
        {
            ctx.Self?.SetDeathBehavior(resolved);
            return NodeStatus.Success;
        }
    }
}
