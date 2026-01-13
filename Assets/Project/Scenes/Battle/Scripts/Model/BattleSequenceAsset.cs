using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Timeline;

namespace Project.Scenes.Battle.Scripts.Model
{
    [CreateAssetMenu(fileName = "BattleSequence", menuName = "Battle/Phase Sequence")]
    public class BattleSequenceAsset : ScriptableObject
    {
        [SerializeField] BattleSequenceType sequenceType = BattleSequenceType.Way;
        [SerializeField] List<BattlePhaseDefinition> phases = new();

        public BattleSequenceType SequenceType => sequenceType;
        public IReadOnlyList<BattlePhaseDefinition> Phases => phases;

#if UNITY_EDITOR
        private void OnValidate()
        {
            // アセット名に"Boss"が含まれていればBoss、そうでなければWay
            bool isBoss = name.Contains("Boss", System.StringComparison.OrdinalIgnoreCase);
            bool isWay = name.Contains("Way", System.StringComparison.OrdinalIgnoreCase);
            if (isBoss && sequenceType != BattleSequenceType.Boss)
            {
                sequenceType = BattleSequenceType.Boss;
                Debug.LogWarning($"BattleSequenceAsset: {name} is automatically set to Boss", this);
                UnityEditor.EditorUtility.SetDirty(this);
            }
            else if (isWay && sequenceType != BattleSequenceType.Way)
            {
                sequenceType = BattleSequenceType.Way;
                Debug.LogWarning($"BattleSequenceAsset: {name} is automatically set to Way", this);
                UnityEditor.EditorUtility.SetDirty(this);
            }
        }
#endif
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
        [SerializeField] BattleTimelineBuilderAsset timelineBuilder;
        [SerializeField] BattlePhaseExitCondition exitCondition = BattlePhaseExitCondition.TimeLimit;

        public string PhaseId => phaseId;
        public float TimeLimitSeconds => timeLimitSeconds;
        public BattleTimelineBuilderAsset TimelineBuilder => timelineBuilder;
        public BattlePhaseExitCondition ExitCondition => exitCondition;

        public TimelineAsset CreateTimeline()
        {
            return timelineBuilder ? timelineBuilder.BuildTimeline() : null;
        }
    }

    public enum BattlePhaseExitCondition
    {
        TimeLimit
    }
}
