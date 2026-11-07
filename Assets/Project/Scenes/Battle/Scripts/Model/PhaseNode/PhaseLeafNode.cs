using System;
using System.Collections.Generic;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Model.PhaseNode
{
    [Serializable]
    public class PhaseLeafNode : IPhaseNode
    {
        [SerializeField] BattlePhaseDefinition phase = new();

        public PhaseLeafNode() { }

        public PhaseLeafNode(BattlePhaseDefinition phase)
        {
            this.phase = phase;
        }

        public BattlePhaseDefinition Phase => phase;

        public string Label
        {
            get
            {
                if (phase == null) return "(empty)";
                if (phase.ActionAsset != null) return phase.ActionAsset.name;
                return string.IsNullOrEmpty(phase.PhaseId) ? "(no action)" : phase.PhaseId;
            }
        }

        public IEnumerable<BattlePhaseDefinition> Enumerate(PhaseNodeContext context)
        {
            if (phase != null)
            {
                yield return phase;
            }
        }
    }
}
