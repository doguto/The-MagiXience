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
        [SerializeReference, SubclassSelector]
        IExitConditionConfig exitConditionConfig = new TimeLimitExitConditionConfig();
        [Tooltip("randomPickグループで、このフェーズの直後に挟むinterludeのTimelineBuilderを差し替える。未設定ならグループ共通のものを使う")]
        [SerializeField] BattleTimelineBuilderAsset interludeBuilderOverride;

        public string PhaseId => phaseId;
        public BattleTimelineBuilderAsset TimelineBuilder => timelineBuilder;
        public BattleTimelineBuilderAsset TimelineBuilderStrong => timelineBuilderStrong;
        public IExitConditionConfig ExitConditionConfig => exitConditionConfig;
        public BattleTimelineBuilderAsset InterludeBuilderOverride => interludeBuilderOverride;

        /// <summary>TimelineBuilderだけを差し替えた浅いコピーを返す（Strong版は使わない）。</summary>
        public BattlePhaseDefinition WithTimelineBuilder(BattleTimelineBuilderAsset builder)
        {
            var copy = (BattlePhaseDefinition)MemberwiseClone();
            copy.timelineBuilder = builder;
            copy.timelineBuilderStrong = null;
            return copy;
        }

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

        [Header("Random Pick")]
        [Tooltip("true の場合、phases を候補として1周ごとに1つをランダム選択する（順番には進まない）")]
        [SerializeField] bool randomPick;
        [Tooltip("randomPick 時、選択したフェーズの直後に毎回挟むフェーズ（原点復帰など）。timelineBuilder が未設定なら挟まない")]
        [SerializeField] BattlePhaseDefinition interlude;
        [Tooltip("randomPick 時、ボスのHPがこの割合(%)以下になったらグループを抜ける。0 = HPでは抜けない（loopCountのみ）")]
        [SerializeField, Range(0f, 100f)] float endBossHpPercent;

        public bool Loop => loop;
        public int LoopCount => loopCount;
        public IReadOnlyList<BattlePhaseDefinition> Phases => phases;
        public bool RandomPick => randomPick;
        public float EndBossHpPercent => endBossHpPercent;
        public BattlePhaseDefinition Interlude => interlude != null && interlude.TimelineBuilder != null ? interlude : null;
    }
}
