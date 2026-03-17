using System.Collections.Generic;
using Project.Scenes.Battle.Scripts.Model.Movement;
using Project.Scripts.Model;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Model
{
    public class BattleSequenceModel
    {
        readonly IReadOnlyList<BattlePhaseModelBase> phases;
        int currentIndex = -1;

        public BattleSequenceModel(
            BattleSituation situation,
            IReadOnlyList<BattlePhaseModelBase> phases,
            GameObject bossPrefab = null,
            Vector3 bossSpawnPosition = default,
            IReadOnlyList<IMovementStep> bossEntranceMovement = null)
        {
            Situation = situation;
            this.phases = phases;
            BossPrefab = bossPrefab;
            BossSpawnPosition = bossSpawnPosition;
            BossEntranceMovement = bossEntranceMovement;
        }

        public BattleSituation Situation { get; }
        public bool HasPhases => phases.Count > 0;
        public IReadOnlyList<BattlePhaseModelBase> Phases => phases;
        public GameObject BossPrefab { get; }
        public Vector3 BossSpawnPosition { get; }
        public IReadOnlyList<IMovementStep> BossEntranceMovement { get; }

        public BattlePhaseModelBase MoveNext()
        {
            var nextIndex = currentIndex + 1;
            if (nextIndex >= phases.Count)
            {
                currentIndex = phases.Count;
                return null;
            }

            currentIndex = nextIndex;
            return phases[currentIndex];
        }

        public void Reset()
        {
            currentIndex = -1;
        }
    }
}
