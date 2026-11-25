using System;
using System.Collections.Generic;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.BehaviorTree
{
    [Serializable]
    public abstract class CompositeNode : Node
    {
        [SerializeReference, HideInInspector] List<Node> children = new();

        public override NodeChildCapacity ChildCapacity => NodeChildCapacity.Multiple;
        public override IReadOnlyList<Node> Children => children;

        public override void SetChildren(IList<Node> newChildren)
        {
            children = new List<Node>(newChildren);
        }

        private protected override void PrepareChildren() => children.RemoveAll(c => c == null);
    }
}
