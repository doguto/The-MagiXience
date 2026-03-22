using System.Collections.Generic;
using Project.Scenes.Battle.Scripts.Model.ExitCondition;
using UniRx;

namespace Project.Scenes.Battle.Scripts.Model
{
    public class CompositeBattlePhaseModel : BattlePhaseModelBase
    {
        readonly CompositeMode mode;
        readonly IReadOnlyList<BattlePhaseModelBase> innerModels;
        readonly HashSet<BattlePhaseModelBase> completedModels = new();

        public CompositeBattlePhaseModel(
            BattlePhaseDefinition definition,
            CompositeMode mode,
            IReadOnlyList<BattlePhaseModelBase> innerModels) : base(definition)
        {
            this.mode = mode;
            this.innerModels = innerModels;
        }

        protected override void OnEnter()
        {
            if (innerModels.Count == 0)
            {
                CompletePhase();
                return;
            }

            foreach (var inner in innerModels)
            {
                inner.Enter(Director);

                inner.OnExitPhase
                    .Take(1)
                    .Subscribe(_ => OnInnerCompleted(inner))
                    .AddTo(Disposables);
            }
        }

        void OnInnerCompleted(BattlePhaseModelBase inner)
        {
            completedModels.Add(inner);

            switch (mode)
            {
                case CompositeMode.Or:
                    CompletePhase();
                    break;

                case CompositeMode.And:
                    if (completedModels.Count >= innerModels.Count)
                    {
                        CompletePhase();
                    }
                    break;
            }
        }

        public override void Exit()
        {
            foreach (var inner in innerModels)
            {
                inner.Exit();
            }
            base.Exit();
        }

        public override void Dispose()
        {
            foreach (var inner in innerModels)
            {
                inner.Dispose();
            }
            base.Dispose();
        }
    }
}
