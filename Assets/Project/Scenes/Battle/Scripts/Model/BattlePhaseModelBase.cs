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
        bool isTimelineResolved;
        TimelineAsset resolvedTimelineStrong;
        bool isTimelineStrongResolved;

        protected BattlePhaseModelBase(BattlePhaseDefinition definition)
        {
            Definition = definition;
        }

        protected BattlePhaseDefinition Definition { get; }
        protected PlayableDirector Director { get; private set; }
        protected CompositeDisposable Disposables { get; } = new();

        public string PhaseId => Definition.PhaseId;
        public BattleTimelineBuilderAsset Builder => Definition.TimelineBuilder;
        public BattleTimelineBuilderAsset BuilderStrong => Definition.TimelineBuilderStrong;
        public TimelineAsset TimelineAsset => ResolveTimeline();
        public IObservable<Unit> OnExitPhase => exitSubject;

        public TimelineAsset ResolveTimeline()
        {
            if (isTimelineResolved)
            {
                return resolvedTimeline;
            }

            isTimelineResolved = true;
            var asset = Definition.CreateTimeline();
            resolvedTimeline = asset ? asset : null;

            return resolvedTimeline;
        }

        public TimelineAsset ResolveTimelineStrong()
        {
            if (isTimelineStrongResolved)
            {
                return resolvedTimelineStrong;
            }

            isTimelineStrongResolved = true;
            var asset = Definition.CreateTimelineStrong();
            resolvedTimelineStrong = asset ? asset : null;

            return resolvedTimelineStrong;
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

            if (resolvedTimeline)
            {
                UnityEngine.Object.Destroy(resolvedTimeline);
            }
            if (resolvedTimelineStrong)
            {
                UnityEngine.Object.Destroy(resolvedTimelineStrong);
            }
        }
    }
}
