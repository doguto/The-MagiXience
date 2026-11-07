using System;
using UniRx;

namespace Project.Scenes.Battle.Scripts.Model
{
    public abstract class BattlePhaseModelBase : IDisposable
    {
        readonly Subject<Unit> exitSubject = new();

        bool isDisposed;

        protected BattlePhaseModelBase(BattlePhaseDefinition definition)
        {
            Definition = definition;
        }

        protected BattlePhaseDefinition Definition { get; }
        protected CompositeDisposable Disposables { get; } = new();

        public string PhaseId => Definition.PhaseId;
        public PhaseActionAsset ActionAsset => Definition.ActionAsset;
        public IObservable<Unit> OnExitPhase => exitSubject;

        public void Enter()
        {
            OnEnter();
        }

        protected abstract void OnEnter();

        public virtual void Exit()
        {
            Disposables.Clear();
        }

        protected void CompletePhase()
        {
            exitSubject.OnNext(Unit.Default);
        }

        // LoopPhaseNode の interrupt から呼ばれる。破棄済みなら何もしない
        public void ForceComplete()
        {
            if (isDisposed) return;
            CompletePhase();
        }

        public virtual void Dispose()
        {
            isDisposed = true;
            Disposables.Dispose();
            exitSubject.Dispose();
        }
    }
}
