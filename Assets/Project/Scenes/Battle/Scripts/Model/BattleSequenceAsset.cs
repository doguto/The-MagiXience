using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Timeline;
using Project.Scripts.Model;
using Project.Scenes.Battle.Scripts.Model.ExitCondition;
using Project.Scenes.Battle.Scripts.Model.Movement;

namespace Project.Scenes.Battle.Scripts.Model
{
    [CreateAssetMenu(fileName = "BattleSequence", menuName = "Battle/Phase Sequence")]
    public class BattleSequenceAsset : ScriptableObject
    {
        [SerializeField] BattleSituation situation = BattleSituation.Way;
        [SerializeField] List<SequenceGroup> sequenceGroups = new();

        [Header("Boss Prefab")]
        [SerializeField] GameObject bossPrefab;
        [SerializeField] Vector3 bossSpawnPosition;
        [SerializeReference, SubclassSelector]
        List<IMovementStep> bossEntranceMovement = new();

        public BattleSituation Situation => situation;
        public IReadOnlyList<SequenceGroup> SequenceGroups => sequenceGroups;
        public GameObject BossPrefab => bossPrefab;
        public Vector3 BossSpawnPosition => bossSpawnPosition;
        public IReadOnlyList<IMovementStep> BossEntranceMovement => bossEntranceMovement;

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
        [SerializeField] BattleTimelineBuilderAsset timelineBuilderStrong;
        [SerializeField, Range(0f, 100f)] float strongAttackHpThresholdPercent = 50f;
        [SerializeReference, SubclassSelector]
        IExitConditionConfig exitConditionConfig = new TimeLimitExitConditionConfig();

        public string PhaseId => phaseId;
        public BattleTimelineBuilderAsset TimelineBuilder => timelineBuilder;
        public BattleTimelineBuilderAsset TimelineBuilderStrong => timelineBuilderStrong;
        public float StrongAttackHpThresholdPercent => strongAttackHpThresholdPercent;
        public IExitConditionConfig ExitConditionConfig => exitConditionConfig;

        public TimelineAsset CreateTimeline()
        {
            return timelineBuilder ? timelineBuilder.BuildTimeline() : null;
        }

        public TimelineAsset CreateTimelineStrong()
        {
            return timelineBuilderStrong ? timelineBuilderStrong.BuildTimeline() : null;
        }
    }

    [Serializable]
    public class SequenceGroup
    {
        [SerializeField] bool loop;
        [SerializeField] int loopCount; // 0 = infinite
        [SerializeField] List<BattlePhaseDefinition> phases = new();

        public bool Loop => loop;
        public int LoopCount => loopCount;
        public IReadOnlyList<BattlePhaseDefinition> Phases => phases;
    }
}
