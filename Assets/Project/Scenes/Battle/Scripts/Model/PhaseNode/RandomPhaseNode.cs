using System;
using System.Collections.Generic;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Model.PhaseNode
{
    [Serializable]
    public class RandomPhaseNode : IPhaseNode
    {
        [SerializeReference, SubclassSelector]
        List<IPhaseNode> children = new();
        [Tooltip("直前に選んだ子を抽選から除外する")]
        [SerializeField] bool noRepeat = true;

        public RandomPhaseNode() { }

        public RandomPhaseNode(List<IPhaseNode> children, bool noRepeat)
        {
            this.children = children;
            this.noRepeat = noRepeat;
        }

        public IReadOnlyList<IPhaseNode> Children => children;

        public string Label => $"Random[{children.Count}]";

        public IEnumerable<BattlePhaseDefinition> Enumerate(PhaseNodeContext context)
        {
            if (children.Count == 0) yield break;

            var excluded = noRepeat ? context.GetLastPickedIndex(this) : -1;
            var index = PickRandomIndexExcluding(children.Count, excluded);
            context.SetLastPickedIndex(this, index);

            var picked = children[index];
            if (picked == null) yield break;

            foreach (var definition in picked.Enumerate(context))
            {
                yield return definition;
            }
        }

        static int PickRandomIndexExcluding(int count, int excluded)
        {
            if (count <= 1 || excluded < 0 || excluded >= count)
            {
                return UnityEngine.Random.Range(0, count);
            }

            var index = UnityEngine.Random.Range(0, count - 1);
            return index >= excluded ? index + 1 : index;
        }
    }
}
