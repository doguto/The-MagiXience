using System.Collections.Generic;

namespace Project.Scenes.Battle.Scripts.Model
{
    public class BattleSequenceModel
    {
        readonly IReadOnlyList<BattlePhaseModelBase> phases;
        int currentIndex = -1;

        public BattleSequenceModel(BattleSequenceType sequenceType, IReadOnlyList<BattlePhaseModelBase> phases)
        {
            SequenceType = sequenceType;
            this.phases = phases;
        }

        public BattleSequenceType SequenceType { get; }
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
