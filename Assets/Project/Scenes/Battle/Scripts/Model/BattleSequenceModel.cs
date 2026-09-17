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
            IReadOnlyList<IMovementStep> bossEntranceMovement = null,
            BattleWindSettings wind = null)
        {
            Situation = situation;
            this.groups = groups;
            this.phaseFactory = phaseFactory;
            BossPrefab = bossPrefab;
            BossSpawnPosition = bossSpawnPosition;
            BossEntranceMovement = bossEntranceMovement;
            Wind = wind;
        }

        public BattleSituation Situation { get; }
        public BattleWindSettings Wind { get; }
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

                currentGroupIndex++;
                if (currentGroupIndex >= groups.Count)
                {
                    return null;
                }

                currentPhaseInGroup = -1;
                currentLoopIteration = 0;
            }
        }

        public void Reset()
        {
            currentGroupIndex = startGroupIndex;
            currentPhaseInGroup = startPhaseInGroup - 1;
            currentLoopIteration = 0;
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
            IReadOnlyList<BattlePhaseDefinition> phases)
        {
            Loop = loop;
            LoopCount = loopCount;
            Phases = phases;
        }
        public bool Loop { get; }
        public int LoopCount { get; } // 0 = infinite
        public IReadOnlyList<BattlePhaseDefinition> Phases { get; }
    }
}
