using System;
using System.Collections.Generic;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.BehaviorTree
{
    // アセットに保存される不変の定義。実行時は Build で毎回 INode を生成する。
    [Serializable]
    public abstract class NodeDefinition
    {
        [SerializeField, HideInInspector] string guid = System.Guid.NewGuid().ToString("N");
        [SerializeField, HideInInspector] Vector2 graphPosition;
        [SerializeField] string comment;

        public string Guid => guid;
        public string Comment => comment;

        public Vector2 GraphPosition
        {
            get => graphPosition;
            set => graphPosition = value;
        }

        public void RegenerateGuid() => guid = System.Guid.NewGuid().ToString("N");

        public abstract INode Build(BuildScope scope);

        public virtual NodeChildCapacity ChildCapacity => NodeChildCapacity.None;
        public virtual IReadOnlyList<NodeDefinition> Children => Array.Empty<NodeDefinition>();
        public virtual void SetChildren(IList<NodeDefinition> children) { }
    }

    public enum NodeChildCapacity
    {
        None,
        Single,
        Multiple,
    }

    [Serializable]
    public abstract class CompositeNodeDefinition : NodeDefinition
    {
        [SerializeReference, HideInInspector] List<NodeDefinition> children = new();

        public override NodeChildCapacity ChildCapacity => NodeChildCapacity.Multiple;
        public override IReadOnlyList<NodeDefinition> Children => children;

        public override void SetChildren(IList<NodeDefinition> newChildren)
        {
            children = new List<NodeDefinition>(newChildren);
        }

        protected INode[] BuildChildren(BuildScope scope)
        {
            var result = new List<INode>(children.Count);
            foreach (var child in children)
            {
                if (child == null) continue;
                result.Add(child.Build(scope));
            }
            return result.ToArray();
        }
    }

    [Serializable]
    public abstract class DecoratorNodeDefinition : NodeDefinition
    {
        [SerializeReference, HideInInspector] NodeDefinition child;

        public NodeDefinition Child => child;
        public override NodeChildCapacity ChildCapacity => NodeChildCapacity.Single;
        public override IReadOnlyList<NodeDefinition> Children => child == null ? Array.Empty<NodeDefinition>() : new[] { child };

        public override void SetChildren(IList<NodeDefinition> newChildren)
        {
            child = newChildren.Count > 0 ? newChildren[0] : null;
        }

        protected INode BuildChild(BuildScope scope) => child?.Build(scope) ?? new SucceedNode();
    }

    [Serializable]
    public abstract class ActionNodeDefinition : NodeDefinition
    {
    }

    sealed class SucceedNode : Node
    {
        protected override NodeStatus OnTick(BattleContext ctx) => NodeStatus.Success;
    }
}
