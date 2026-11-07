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
        bool isDisposed;

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

        /// <summary>このフェーズが生成したTimelineAssetかどうか(未生成なら生成しない)</summary>
        public bool OwnsTimeline(PlayableAsset asset)
        {
            if (!asset) return false;
            return asset == resolvedTimeline || asset == resolvedTimelineStrong;
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
