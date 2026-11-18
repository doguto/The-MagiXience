using System;
using System.Collections.Generic;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.BehaviorTree
{
    [Serializable, NodeInfo("別の BehaviorTree を実行する。パラメータを上書きできる")]
    public sealed class SubTree : ActionNode
    {
        [SerializeField] BehaviorTreeAsset tree;
        [SerializeField, Tooltip("ツリーの Object パラメータ名。指定するとその値のツリーを実行する (値が空なら何もしない)")] string treeParam;
        [SerializeField] List<TreeParameter> overrides = new();

        [NonSerialized] Node root;

        public BehaviorTreeAsset Tree
        {
            get => tree;
            set => tree = value;
        }

        public string TreeParam
        {
            get => treeParam;
            set => treeParam = value;
        }

        public List<TreeParameter> Overrides => overrides;

        public override string Summary =>
            (!string.IsNullOrEmpty(treeParam) ? "$" + treeParam : tree ? tree.name : "(未設定)") + (overrides.Count > 0 ? $"  ({overrides.Count} override)" : "");

        protected override void OnSetup(TreeScope scope)
        {
            var resolved = ParamObjects.Resolve(scope, treeParam, tree);
            if (resolved == null && string.IsNullOrEmpty(treeParam)) Debug.LogWarning("[BehaviorTree] SubTree asset is not assigned.");
            root = resolved != null ? resolved.Instantiate(scope, overrides) : null;
        }

        protected override NodeStatus OnTick(BattleContext ctx) => root?.Tick(ctx) ?? NodeStatus.Success;
        protected override void OnAbort(BattleContext ctx) => root?.Abort(ctx);
        protected override void OnRecycle() => root?.Recycle();
    }
}
