using System.Collections.Generic;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.BehaviorTree
{
    [CreateAssetMenu(fileName = "BehaviorTree", menuName = "Battle/Behavior Tree")]
    public class BehaviorTreeAsset : ScriptableObject
    {
        [SerializeReference] NodeDefinition root;
        [SerializeReference] List<NodeDefinition> detachedNodes = new();
        [SerializeField] List<TreeParameter> parameters = new();

        public NodeDefinition Root
        {
            get => root;
            set => root = value;
        }

        // エディタ上で親に未接続のノード。実行時には使用しない。
        public List<NodeDefinition> DetachedNodes => detachedNodes;
        public List<TreeParameter> Parameters => parameters;

        public INode Build(BuildScope parentScope = null, IReadOnlyList<TreeParameter> overrides = null)
        {
            var scope = BuildScope.Enter(parentScope, this, overrides);
            return root?.Create(scope) ?? new SucceedNode();
        }
    }
}
