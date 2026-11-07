using System;
using System.Collections.Generic;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Model.PhaseNode
{
    [Serializable]
    public class SequencePhaseNode : IPhaseNode
    {
        [SerializeReference, SubclassSelector]
        List<IPhaseNode> children = new();

        public SequencePhaseNode() { }

        public SequencePhaseNode(List<IPhaseNode> children)
        {
            this.children = children;
        }

        public IReadOnlyList<IPhaseNode> Children => children;

        public string Label => $"Sequence[{children.Count}]";

        public IEnumerable<BattlePhaseDefinition> Enumerate(PhaseNodeContext context) => Enumerate(context, 0);

        public IEnumerable<BattlePhaseDefinition> Enumerate(PhaseNodeContext context, int startIndex)
        {
            for (var i = Mathf.Max(0, startIndex); i < children.Count; i++)
            {
                var child = children[i];
                if (child == null) continue;

                foreach (var definition in child.Enumerate(context))
                {
                    yield return definition;
                }
            }
        }
    }
}
