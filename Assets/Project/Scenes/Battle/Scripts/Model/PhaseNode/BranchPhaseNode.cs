using System;
using System.Collections.Generic;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Model.PhaseNode
{
    // 到達した時点で条件を評価し、どちらか一方の子を実行する
    [Serializable]
    public class BranchPhaseNode : IPhaseNode
    {
        [SerializeReference, SubclassSelector]
        IBranchCondition condition;
        [SerializeReference, SubclassSelector]
        IPhaseNode whenTrue;
        [SerializeReference, SubclassSelector]
        IPhaseNode whenFalse;

        public BranchPhaseNode() { }

        public BranchPhaseNode(IBranchCondition condition, IPhaseNode whenTrue, IPhaseNode whenFalse)
        {
            this.condition = condition;
            this.whenTrue = whenTrue;
            this.whenFalse = whenFalse;
        }

        public string Label => $"Branch({condition?.GetType().Name}) {whenFalse?.Label}";

        public IEnumerable<BattlePhaseDefinition> Enumerate(PhaseNodeContext context)
        {
            var selected = condition != null && condition.Evaluate(context) ? whenTrue : whenFalse;
            if (selected == null) yield break;

            foreach (var definition in selected.Enumerate(context))
            {
                yield return definition;
            }
        }
    }
}
