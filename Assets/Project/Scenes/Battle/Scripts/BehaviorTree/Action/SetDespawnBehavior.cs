using System;
using Project.Scenes.Battle.Scripts.Runtime;
using Project.Scripts.Extensions;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.BehaviorTree
{
    [Serializable, NodeInfo("自分が消えるときに 1 回だけ実行するツリーを設定する。指定した理由で消えたときだけ実行する", "Action")]
    public sealed class SetDespawnBehavior : ActionNode
    {
        [SerializeField] BehaviorTreeAsset tree;
        [SerializeField, Tooltip("ツリーの Object パラメータ名。指定するとその値のツリーを使う (値が空なら何もしない)")] string treeParam;
        [SerializeField, Tooltip("このどれかの理由で消えたときだけ実行する。一括消去 (Cleared) は普通は含めない")] DespawnReason reasons = DespawnReason.Defeated;

        [NonSerialized] BehaviorTreeAsset resolved;

        public override string Summary
        {
            get
            {
                var target = !string.IsNullOrEmpty(treeParam) ? "$" + treeParam : tree ? tree.name : null;
                return target != null ? $"消えるとき ({reasons}): {target}" : "消えるときの行動なし";
            }
        }

        protected override void OnSetup(TreeScope scope) => resolved = ParamObjects.Resolve(scope, treeParam, tree);

        protected override NodeStatus OnTick(BattleContext ctx)
        {
            ctx.Self?.SetDespawnBehavior(resolved, reasons);
            return NodeStatus.Success;
        }
    }
}
