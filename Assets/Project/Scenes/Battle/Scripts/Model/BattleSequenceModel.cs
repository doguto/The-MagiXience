using System;
using System.Collections.Generic;
using Project.Scenes.Battle.Scripts.Model.Entity;
using Project.Scenes.Battle.Scripts.Model.ExitCondition;
using Project.Scenes.Battle.Scripts.Model.Movement;
using Project.Scenes.Battle.Scripts.Model.PhaseNode;
using Project.Scripts.Model;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Model
{
    public class BattleSequenceModel : IDisposable
    {
        readonly IPhaseNode root;
        readonly Func<BattlePhaseDefinition, BattlePhaseModelBase> phaseFactory;
        readonly Func<IExitConditionConfig, BattlePhaseModelBase> watcherFactory;
        readonly Func<EntityBase> getBossModel;
        readonly List<BattlePhaseModelBase> allCreatedPhases = new();

        IEnumerator<BattlePhaseDefinition> enumerator;
        BattlePhaseModelBase currentPhase;

        // Reset() 時に再生を始める root 直下の子の位置。通常は0、デバッグ起動時のみ途中を指す。
        int startIndex;

        public BattleSequenceModel(
            BattleSituation situation,
            IPhaseNode root,
            Func<BattlePhaseDefinition, BattlePhaseModelBase> phaseFactory,
            Func<IExitConditionConfig, BattlePhaseModelBase> watcherFactory,
            Func<EntityBase> getBossModel,
            GameObject bossPrefab = null,
            Vector3 bossSpawnPosition = default,
            IReadOnlyList<IMovementStep> bossEntranceMovement = null,
            BattleWindSettings wind = null)
        {
            Situation = situation;
            this.root = root;
            this.phaseFactory = phaseFactory;
            this.watcherFactory = watcherFactory;
            this.getBossModel = getBossModel;
            BossPrefab = bossPrefab;
            BossSpawnPosition = bossSpawnPosition;
            BossEntranceMovement = bossEntranceMovement;
            Wind = wind;
        }

        public BattleSituation Situation { get; }
        public BattleWindSettings Wind { get; }
        public bool HasPhases => root != null;
        public IReadOnlyList<BattlePhaseModelBase> AllCreatedPhases => allCreatedPhases;
        public GameObject BossPrefab { get; }
        public Vector3 BossSpawnPosition { get; }
        public IReadOnlyList<IMovementStep> BossEntranceMovement { get; }

        public BattlePhaseModelBase MoveNext()
        {
            // enumerator を進める間に Loop の終了条件が即成立して interrupt が飛んでも、
            // 直前のフェーズを打ち切らないよう先に外しておく
            currentPhase = null;

            enumerator ??= CreateEnumerator();
            if (enumerator == null || !enumerator.MoveNext())
            {
                return null;
            }

            currentPhase = phaseFactory(enumerator.Current);
            allCreatedPhases.Add(currentPhase);
            return currentPhase;
        }

        IEnumerator<BattlePhaseDefinition> CreateEnumerator()
        {
            if (root == null) return null;

            var context = new PhaseNodeContext(watcherFactory, InterruptCurrentPhase, getBossModel);
            var definitions = root is SequencePhaseNode sequence
                ? sequence.Enumerate(context, startIndex)
                : root.Enumerate(context);
            return definitions.GetEnumerator();
        }

        void InterruptCurrentPhase()
        {
            currentPhase?.ForceComplete();
        }

        /// <summary>
        /// 終了済みのフェーズを破棄して保持リストから外す。ループするグループではフェーズ遷移の度に
        /// TimelineAssetが生成されるため、シーケンス終了まで溜め込まないようにする。
        /// </summary>
        public void ReleasePhase(BattlePhaseModelBase phase)
        {
            if (phase == null || phase == currentPhase) return;
            if (!allCreatedPhases.Remove(phase)) return;
            phase.Dispose();
        }

        public void Reset()
        {
            // 途中で止めた enumerator を破棄すると、各 Loop の finally で終了条件の監視も破棄される
            ReleaseEnumerator();
            currentPhase = null;
        }

        public void Dispose()
        {
            ReleaseEnumerator();
            currentPhase = null;
        }

        void ReleaseEnumerator()
        {
            enumerator?.Dispose();
            enumerator = null;
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        /// <summary>
        /// デバッグ用: root 直下の子の位置でシーケンスの開始位置を指定する。
        /// root が Sequence でない場合は先頭からのみ。指定後は Reset() のたびにその位置から再生される。
        /// </summary>
        public void SetStartIndex(int index)
        {
            startIndex = 0;
            if (index <= 0) return;

            if (root is SequencePhaseNode sequence && index < sequence.Children.Count)
            {
                startIndex = index;
                return;
            }

            Debug.LogWarning($"[BattleSequenceModel] Start index {index} is out of range. Falling back to the first phase.");
        }
#endif
    }
}
