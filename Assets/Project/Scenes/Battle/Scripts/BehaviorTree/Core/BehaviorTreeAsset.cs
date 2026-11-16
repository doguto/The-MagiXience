using System.Collections.Generic;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.BehaviorTree
{
    [CreateAssetMenu(fileName = "BehaviorTree", menuName = "Battle/Behavior Tree")]
    public class BehaviorTreeAsset : ScriptableObject
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
