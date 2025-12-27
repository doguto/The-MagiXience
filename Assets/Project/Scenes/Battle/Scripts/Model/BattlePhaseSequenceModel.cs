using System.Collections.Generic;

namespace Project.Scenes.Battle.Scripts.Model
{
    public class BattlePhaseSequenceModel
    {
        readonly IReadOnlyList<BattlePhaseModelBase> phases;
        int currentIndex = -1;

        public BattlePhaseSequenceModel(BattleSequenceType sequenceType, IReadOnlyList<BattlePhaseModelBase> phases)
        {
            SequenceType = sequenceType;
            this.phases = phases;
        }

        public BattleSequenceType SequenceType { get; }
        public bool HasPhases => phases.Count > 0;
        public IReadOnlyList<BattlePhaseModelBase> Phases => phases;

        public bool TryMoveNext(out BattlePhaseModelBase phase)
        {
            var nextIndex = currentIndex + 1;
            if (nextIndex >= phases.Count)
            {
                phase = null;
                currentIndex = phases.Count;
                return false;
            }

            currentIndex = nextIndex;
            phase = phases[currentIndex];
            return true;
        }

        public void Reset()
        {
            currentIndex = -1;
        }
    }
}
