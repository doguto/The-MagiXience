using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using Project.Scripts.Model;
using Project.Scenes.Battle.Scripts.Model.ExitCondition;
using Project.Scenes.Battle.Scripts.Model.Movement;
using Project.Scenes.Battle.Scripts.Model.PhaseNode;

namespace Project.Scenes.Battle.Scripts.Model
{
    [CreateAssetMenu(fileName = "BattleSequence", menuName = "Battle/Phase Sequence")]
    public class BattleSequenceAsset : ScriptableObject
    {
        [SerializeField] BattleSituation situation = BattleSituation.Way;
        [SerializeField] List<SequenceGroup> sequenceGroups = new();
        [Header("Phase Tree")]
        [Tooltip("設定されている場合は sequenceGroups より優先して使用する")]
        [SerializeReference, SubclassSelector]
        IPhaseNode root;
        [Header("Wind")]
        [SerializeField] BattleWindSettings wind = new();
        public BattleWindSettings Wind => wind;

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

        /// <summary>
        /// root が未設定なら、旧形式の sequenceGroups を同じ挙動のツリーへ変換して返す。
        /// 順番に並ぶだけのグループは root 直下へ展開する（デバッグの開始位置は root 直下の子単位のため）。
        /// </summary>
        public IPhaseNode BuildRootNode()
        {
            if (root != null) return root;

            var children = new List<IPhaseNode>();
            foreach (var group in sequenceGroups)
            {
                if (group.RandomPick && group.Phases.Count > 0)
                {
                    children.Add(ConvertRandomGroup(group));
                }
                else if (group.Loop)
                {
                    children.Add(new LoopPhaseNode(new SequencePhaseNode(ToLeaves(group.Phases)), group.LoopCount));
                }
                else
                {
                    children.AddRange(ToLeaves(group.Phases));
                }
            }
            return new SequencePhaseNode(children);
        }

        static List<IPhaseNode> ToLeaves(IReadOnlyList<BattlePhaseDefinition> phases)
        {
            var leaves = new List<IPhaseNode>(phases.Count);
            foreach (var phase in phases)
            {
                leaves.Add(new PhaseLeafNode(phase));
            }
            return leaves;
        }

        static IPhaseNode ConvertRandomGroup(SequenceGroup group)
        {
            var options = new List<IPhaseNode>(group.Phases.Count);
            foreach (var phase in group.Phases)
            {
                var interlude = ResolveInterlude(group, phase);
                options.Add(interlude == null
                    ? new PhaseLeafNode(phase)
                    : new SequencePhaseNode(new List<IPhaseNode> { new PhaseLeafNode(phase), new PhaseLeafNode(interlude) }));
            }

            var exitCondition = group.EndBossHpPercent > 0f
                ? new BossHpThresholdExitConditionConfig(group.EndBossHpPercent)
                : null;
            return new LoopPhaseNode(new RandomPhaseNode(options, noRepeat: true), group.Loop ? group.LoopCount : 1, exitCondition);
        }

        static BattlePhaseDefinition ResolveInterlude(SequenceGroup group, BattlePhaseDefinition picked)
        {
            if (group.Interlude == null) return null;

            return picked.InterludeActionOverride != null
                ? group.Interlude.WithActionAsset(picked.InterludeActionOverride, picked.InterludeTimeLimitOverride)
                : group.Interlude;
        }

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
        [FormerlySerializedAs("timelineBuilder")]
        [SerializeField] PhaseActionAsset actionAsset;
        [SerializeReference, SubclassSelector]
        IExitConditionConfig exitConditionConfig = new TimeLimitExitConditionConfig();
        [Tooltip("randomPickグループで、このフェーズの直後に挟むinterludeのアクションを差し替える。未設定ならグループ共通のものを使う")]
        [FormerlySerializedAs("interludeBuilderOverride")]
        [SerializeField] PhaseActionAsset interludeActionOverride;
        [Tooltip("interludeActionOverride 使用時の終了時間(秒)。0ならグループ共通のinterludeの終了条件を使う")]
        [SerializeField, Min(0f)] float interludeTimeLimitOverride;

        public string PhaseId => phaseId;
        public PhaseActionAsset ActionAsset => actionAsset;
        public IExitConditionConfig ExitConditionConfig => exitConditionConfig;
        public PhaseActionAsset InterludeActionOverride => interludeActionOverride;
        public float InterludeTimeLimitOverride => interludeTimeLimitOverride;

        /// <summary>
        /// アクションを差し替えた浅いコピーを返す。
        /// timeLimitSeconds が0より大きければ、終了条件も時間制限に差し替える。
        /// </summary>
        public BattlePhaseDefinition WithActionAsset(PhaseActionAsset asset, float timeLimitSeconds = 0f)
        {
            var copy = (BattlePhaseDefinition)MemberwiseClone();
            copy.actionAsset = asset;
            if (timeLimitSeconds > 0f)
            {
                copy.exitConditionConfig = new TimeLimitExitConditionConfig(timeLimitSeconds);
            }
            return copy;
        }

        // LoopPhaseNode の終了条件の監視用。アクションを持たない
        public static BattlePhaseDefinition ForCondition(string phaseId, IExitConditionConfig config)
        {
            return new BattlePhaseDefinition { phaseId = phaseId, exitConditionConfig = config };
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
        [Tooltip("randomPick 時、選択したフェーズの直後に毎回挟むフェーズ（原点復帰など）。actionAsset が未設定なら挟まない")]
        [SerializeField] BattlePhaseDefinition interlude;
        [Tooltip("randomPick 時、ボスのHPがこの割合(%)以下になったらグループを抜ける。0 = HPでは抜けない（loopCountのみ）")]
        [SerializeField, Range(0f, 100f)] float endBossHpPercent;

        public bool Loop => loop;
        public int LoopCount => loopCount;
        public IReadOnlyList<BattlePhaseDefinition> Phases => phases;
        public bool RandomPick => randomPick;
        public float EndBossHpPercent => endBossHpPercent;
        public BattlePhaseDefinition Interlude => interlude != null && interlude.ActionAsset != null ? interlude : null;
    }
}
