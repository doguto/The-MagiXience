using System;
using System.Collections.Generic;
using Project.Scenes.Battle.Scripts.Model.Movement;
using Project.Scripts.Model;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Model
{
    public class BattleSequenceModel
    {
        readonly IReadOnlyList<SequenceGroupRuntime> groups;
        readonly Func<BattlePhaseDefinition, BattlePhaseModelBase> phaseFactory;
        readonly List<BattlePhaseModelBase> allCreatedPhases = new();

        int currentGroupIndex = -1;
        int currentPhaseInGroup = -1;
        int currentLoopIteration;
        BattlePhaseDefinition pendingInterlude;
        BattlePhaseModelBase currentPhase;

        // Reset() 時に巻き戻す位置。通常は先頭(0, 0)だが、デバッグ起動時のみ途中を指す。
        int startGroupIndex;
        int startPhaseInGroup;

        public BattleSequenceModel(
            BattleSituation situation,
            IReadOnlyList<SequenceGroupRuntime> groups,
            Func<BattlePhaseDefinition, BattlePhaseModelBase> phaseFactory,
            GameObject bossPrefab = null,
            Vector3 bossSpawnPosition = default,
            IReadOnlyList<IMovementStep> bossEntranceMovement = null)
        {
            Situation = situation;
            this.groups = groups;
            this.phaseFactory = phaseFactory;
            BossPrefab = bossPrefab;
            BossSpawnPosition = bossSpawnPosition;
            BossEntranceMovement = bossEntranceMovement;
        }

        public BattleSituation Situation { get; }
        public bool HasPhases => groups.Count > 0;
        public IReadOnlyList<BattlePhaseModelBase> AllCreatedPhases => allCreatedPhases;
        public GameObject BossPrefab { get; }
        public Vector3 BossSpawnPosition { get; }
        public IReadOnlyList<IMovementStep> BossEntranceMovement { get; }

        public BattlePhaseModelBase MoveNext()
        {
            currentPhase = null;

            while (true)
            {
                if (currentGroupIndex >= 0 && currentGroupIndex < groups.Count)
                {
                    var group = groups[currentGroupIndex];

                    if (group.RandomPick && group.Phases.Count > 0)
                    {
                        if (TryMoveNextRandom(group))
                        {
                            return currentPhase;
                        }
                    }
                    else
                    {
                        var nextPhaseIndex = currentPhaseInGroup + 1;

                        if (nextPhaseIndex < group.Phases.Count)
                        {
                            currentPhaseInGroup = nextPhaseIndex;
                            currentPhase = phaseFactory(group.Phases[currentPhaseInGroup]);
                            allCreatedPhases.Add(currentPhase);
                            return currentPhase;
                        }

                        if (group.Loop)
                        {
                            currentLoopIteration++;
                            var shouldLoop = group.LoopCount == 0 || currentLoopIteration < group.LoopCount;
                            if (shouldLoop)
                            {
                                currentPhaseInGroup = 0;
                                currentPhase = phaseFactory(group.Phases[0]);
                                allCreatedPhases.Add(currentPhase);
                                return currentPhase;
                            }
                        }
                    }
                }

                currentGroupIndex++;
                if (currentGroupIndex >= groups.Count)
                {
                    return null;
                }

                currentPhaseInGroup = -1;
                currentLoopIteration = 0;
                pendingInterlude = null;
            }
        }

        /// <summary>
        /// randomPick グループ用。1周 = 「ランダム選択したフェーズ → (あれば) interlude」。
        /// 周回判定は通常グループと同じく Loop / LoopCount に従う。続きが無ければ false を返す。
        /// </summary>
        bool TryMoveNextRandom(SequenceGroupRuntime group)
        {
            if (pendingInterlude != null)
            {
                var interlude = pendingInterlude;
                pendingInterlude = null;
                return SetCurrentPhase(interlude);
            }

            if (group.ShouldEndGroup != null && group.ShouldEndGroup())
            {
                return false;
            }

            // currentPhaseInGroup >= 0 は「1周分の選択を消化済み」を意味する
            if (currentPhaseInGroup >= 0)
            {
                currentLoopIteration++;
                var shouldLoop = group.Loop && (group.LoopCount == 0 || currentLoopIteration < group.LoopCount);
                if (!shouldLoop)
                {
                    return false;
                }
            }

            currentPhaseInGroup = PickRandomIndexExcluding(group.Phases.Count, currentPhaseInGroup);
            var picked = group.Phases[currentPhaseInGroup];
            pendingInterlude = ResolveInterlude(group, picked);
            return SetCurrentPhase(picked);
        }

        /// <summary>
        /// 直前に選んだindex(excluded)を除外して抽選する。候補が1つ以下、またはexcludedが範囲外(未選択)なら通常抽選。
        /// </summary>
        static int PickRandomIndexExcluding(int count, int excluded)
        {
            if (count <= 1 || excluded < 0 || excluded >= count)
            {
                return UnityEngine.Random.Range(0, count);
            }

            var index = UnityEngine.Random.Range(0, count - 1);
            return index >= excluded ? index + 1 : index;
        }

        static BattlePhaseDefinition ResolveInterlude(SequenceGroupRuntime group, BattlePhaseDefinition picked)
        {
            if (group.Interlude == null)
            {
                return null;
            }

            return picked.InterludeBuilderOverride != null
                ? group.Interlude.WithTimelineBuilder(picked.InterludeBuilderOverride, picked.InterludeTimeLimitOverride)
                : group.Interlude;
        }

        bool SetCurrentPhase(BattlePhaseDefinition definition)
        {
            currentPhase = phaseFactory(definition);
            allCreatedPhases.Add(currentPhase);
            return true;
        }

        public void Reset()
        {
            currentGroupIndex = startGroupIndex;
            currentPhaseInGroup = startPhaseInGroup - 1;
            currentLoopIteration = 0;
            pendingInterlude = null;
            currentPhase = null;
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        /// <summary>
        /// デバッグ用: 全グループを通した連番でシーケンスの開始フェーズを指定する。
        /// 指定後は Reset() のたびにその位置から再生される（リトライ時も同じ位置から）。
        /// </summary>
        public void SetStartPhaseByFlatIndex(int flatIndex)
        {
            startGroupIndex = 0;
            startPhaseInGroup = 0;
            if (flatIndex <= 0) return;

            var remaining = flatIndex;
            for (var i = 0; i < groups.Count; i++)
            {
                var phaseCount = groups[i].Phases.Count;
                if (remaining < phaseCount)
                {
                    startGroupIndex = i;
                    startPhaseInGroup = remaining;
                    return;
                }

                remaining -= phaseCount;
            }

            Debug.LogWarning($"[BattleSequenceModel] Start phase index {flatIndex} is out of range. Falling back to the first phase.");
        }
#endif
    }

    public class SequenceGroupRuntime
    {
        public SequenceGroupRuntime(
            bool loop,
            int loopCount,
            IReadOnlyList<BattlePhaseDefinition> phases,
            bool randomPick = false,
            BattlePhaseDefinition interlude = null,
            Func<bool> shouldEndGroup = null)
        {
            Loop = loop;
            LoopCount = loopCount;
            Phases = phases;
            RandomPick = randomPick;
            Interlude = interlude;
            ShouldEndGroup = shouldEndGroup;
        }
        public bool Loop { get; }
        public int LoopCount { get; } // 0 = infinite
        public IReadOnlyList<BattlePhaseDefinition> Phases { get; }
        public bool RandomPick { get; }
        public BattlePhaseDefinition Interlude { get; }
        // randomPick 時、1周の区切りで true を返すとループ回数に関わらずグループを抜ける（ボスHP条件など）
        public Func<bool> ShouldEndGroup { get; }
    }
}
