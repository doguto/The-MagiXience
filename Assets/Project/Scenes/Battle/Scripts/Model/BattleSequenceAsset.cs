using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Timeline;
using Project.Scripts.Model;
using Project.Scenes.Battle.Scripts.Model.ExitCondition;

namespace Project.Scenes.Battle.Scripts.Model
{
    [CreateAssetMenu(fileName = "BattleSequence", menuName = "Battle/Phase Sequence")]
    public class BattleSequenceAsset : ScriptableObject
    {
        [SerializeField] BattleSituation situation = BattleSituation.Way;
        [SerializeField] List<BattlePhaseDefinition> phases = new();

        public BattleSituation Situation => situation;
        public IReadOnlyList<BattlePhaseDefinition> Phases => phases;

#if UNITY_EDITOR
        private void OnValidate()
        {
            // アセット名に"Boss"が含まれていればBoss、そうでなければWay
            bool isBoss = name.Contains("Boss", StringComparison.OrdinalIgnoreCase);
            bool isWay = name.Contains("Way", StringComparison.OrdinalIgnoreCase);
            if (isBoss && situation != BattleSituation.Boss)
            {
                situation = BattleSituation.Boss;
                Debug.LogWarning($"BattleSequenceAsset: {name} is automatically set to Boss", this);
                UnityEditor.EditorUtility.SetDirty(this);
            }
            else if (isWay && situation != BattleSituation.Way)
            {
                situation = BattleSituation.Way;
                Debug.LogWarning($"BattleSequenceAsset: {name} is automatically set to Way", this);
                UnityEditor.EditorUtility.SetDirty(this);
            }
        }
#endif
    }

    [Serializable]
    public class BattlePhaseDefinition
    {
        [SerializeField] string phaseId;
        [Header("Timeline")]
        [SerializeField] BattleTimelineBuilderAsset timelineBuilder;
        [SerializeReference, SubclassSelector]
        IExitConditionConfig exitConditionConfig = new TimeLimitExitConditionConfig();

        public string PhaseId => phaseId;
        public BattleTimelineBuilderAsset TimelineBuilder => timelineBuilder;
        public IExitConditionConfig ExitConditionConfig => exitConditionConfig;

        public TimelineAsset CreateTimeline()
        {
            return timelineBuilder ? timelineBuilder.BuildTimeline() : null;
        }
    }
}
