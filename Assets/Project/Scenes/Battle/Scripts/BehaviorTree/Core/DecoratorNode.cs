using System;
using System.Collections.Generic;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.BehaviorTree
{
    [Serializable]
    public abstract class DecoratorNode : Node
    {
        [SerializeReference, HideInInspector] Node child;

        public Node Child => child;
        public override NodeChildCapacity ChildCapacity => NodeChildCapacity.Single;
        public override IReadOnlyList<Node> Children => child == null ? Array.Empty<Node>() : new[] { child };

        public override void SetChildren(IList<Node> newChildren)
        {
            child = newChildren.Count > 0 ? newChildren[0] : null;
        }

        private protected override void PrepareChildren() => child ??= new EmptyNode();
    }
}
