using System.Collections.Generic;
using Project.Scripts.Model;

namespace Project.Scenes.Battle.Scripts.Model
{
    public class BattleSequenceModel
    {
        readonly IReadOnlyList<BattlePhaseModelBase> phases;
        int currentIndex = -1;

        public BattleSequenceModel(BattleSituation situation, IReadOnlyList<BattlePhaseModelBase> phases)
        {
            Situation = situation;
            this.phases = phases;
        }

        public BattleSituation Situation { get; }
        public bool HasPhases => phases.Count > 0;
        public IReadOnlyList<BattlePhaseModelBase> Phases => phases;

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
