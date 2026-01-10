using System;
using UniRx;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace Project.Scenes.Battle.Scripts.Model
{
    public abstract class BattlePhaseModelBase : IDisposable
    {
        readonly Subject<Unit> exitSubject = new();

        TimelineAsset resolvedTimeline;
        bool isRuntimeTimeline;
        bool isTimelineResolved;

        protected BattlePhaseModelBase(BattlePhaseDefinition definition)
        {
            Definition = definition;
        }

        protected BattlePhaseDefinition Definition { get; }
        protected PlayableDirector Director { get; private set; }
        protected CompositeDisposable Disposables { get; } = new();

        public string PhaseId => Definition.PhaseId;
        public TimelineAsset TimelineAsset => ResolveTimeline();
        public float TimeLimitSeconds => Definition.TimeLimitSeconds;
        public IObservable<Unit> OnExitPhase => exitSubject;

        public TimelineAsset ResolveTimeline()
        {
            if (isTimelineResolved)
            {
                return resolvedTimeline;
            }

            isTimelineResolved = true;
            if (Definition.TryCreateTimeline(out var asset, out var runtime))
            {
                resolvedTimeline = asset;
                isRuntimeTimeline = runtime;
            }
            else
            {
                resolvedTimeline = null;
                isRuntimeTimeline = false;
            }

            return resolvedTimeline;
        }

        public void Enter(PlayableDirector director)
        {
            Director = director;
            OnEnter();
        }

        protected abstract void OnEnter();

        public virtual void Exit()
        {
            Director = null;
            Disposables.Clear();
        }

        protected void CompletePhase()
        {
            exitSubject.OnNext(Unit.Default);
        }

        public virtual void Dispose()
        {
            Disposables.Dispose();
            exitSubject.Dispose();

            if (isRuntimeTimeline && resolvedTimeline)
            {
                UnityEngine.Object.Destroy(resolvedTimeline);
            }
        }
    }
}
