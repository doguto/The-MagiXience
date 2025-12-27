using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Timeline;

namespace Project.Scenes.Battle.Scripts.Model
{
    [CreateAssetMenu(fileName = "BattlePhaseSequence", menuName = "Battle/Phase Sequence")]
    public class BattlePhaseSequenceAsset : ScriptableObject
    {
        [SerializeField] BattleSequenceType sequenceType = BattleSequenceType.Way;
        [SerializeField] List<BattlePhaseDefinition> phases = new();

        public BattleSequenceType SequenceType => sequenceType;
        public IReadOnlyList<BattlePhaseDefinition> Phases => phases;
    }

    public enum BattleSequenceType
    {
        Way,
        Boss
    }

    [Serializable]
    public class BattlePhaseDefinition
    {
        [SerializeField] string phaseId;
        [SerializeField, Min(0.1f)] float timeLimitSeconds = 10f;
        [Header("Timeline")]
        [SerializeField] TimelineAsset timelineAsset;
        [SerializeField] BattleTimelineBuilderAsset timelineBuilder;
        [SerializeField] BattlePhaseExitCondition exitCondition = BattlePhaseExitCondition.TimeLimit;

        public string PhaseId => phaseId;
        public float TimeLimitSeconds => timeLimitSeconds;
        public TimelineAsset TimelineAsset => timelineAsset;
        public BattleTimelineBuilderAsset TimelineBuilder => timelineBuilder;
        public BattlePhaseExitCondition ExitCondition => exitCondition;

        public bool TryCreateTimeline(out TimelineAsset asset, out bool isRuntime)
        {
            if (timelineAsset)
            {
                asset = timelineAsset;
                isRuntime = false;
                return true;
            }

            if (timelineBuilder)
            {
                asset = timelineBuilder.BuildTimeline();
                isRuntime = true;
                return asset;
            }

            asset = null;
            isRuntime = false;
            return false;
        }
    }

    public enum BattlePhaseExitCondition
    {
        TimeLimit
    }
}
