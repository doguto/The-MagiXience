using System;
using System.Collections.Generic;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.BehaviorTree
{
    // ツリーの種類 (ステージ / 敵 / 弾 / どこでも使える共通) ごとに、置けるノードと SubTree で使えるツリーが決まる
    public abstract class BehaviorTreeAsset : ScriptableObject
    {
        [SerializeReference] Node root;
        [SerializeReference] List<Node> detachedNodes = new();
        [SerializeField] List<TreeParameter> parameters = new();

        public Node Root
        {
            get => root;
            set => root = value;
        }

        // エディタ上で親に未接続のノード。実行時には使用しない。
        public List<Node> DetachedNodes => detachedNodes;
        public List<TreeParameter> Parameters => parameters;

        public abstract bool Accepts(Type nodeType);

        public virtual bool CanUse(BehaviorTreeAsset subTree) => subTree is CommonTreeAsset || subTree.GetType() == GetType();

        protected static bool RequiresEnemy(Type nodeType) => typeof(EnemyActionNode).IsAssignableFrom(nodeType);

        // 実行用の複製を作り、パラメータを解決する。アセット上のノードは書き換えない
        public Node Instantiate(TreeScope parentScope = null, IReadOnlyList<TreeParameter> overrides = null)
        {
            var scope = TreeScope.Enter(parentScope, this, overrides);
            var copy = root != null ? NodeCloner.Clone(root) : new EmptyNode();
            copy.Setup(scope);
            return copy;
        }
    }
}
