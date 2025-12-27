using System;
using UniRx;
using UnityEngine;
using UnityEngine.Playables;
using Project.Scenes.Battle.Scripts.Model;

namespace Project.Scenes.Battle.Scripts.Presenter
{
    public class BattlePhaseStateMachine : MonoBehaviour
    {
        [SerializeField] PlayableDirector playableDirector;
        [SerializeField] BattleTimelineBindingMap bindingMap;

        readonly Subject<BattlePhaseModelBase> phaseStarted = new();
        readonly Subject<BattleSequenceType> sequenceCompleted = new();

        BattlePhaseSequenceModel activeSequence;
        BattlePhaseModelBase activePhase;
        IDisposable exitSubscription;

        public IObservable<BattlePhaseModelBase> OnPhaseStarted => phaseStarted;
        public IObservable<BattleSequenceType> OnSequenceCompleted => sequenceCompleted;

        public void PlaySequence(BattlePhaseSequenceModel sequence)
        {
            if (sequence == null)
            {
                Debug.LogWarning("Sequence is null.", this);
                return;
            }

            Stop();
            activeSequence = sequence;
            activeSequence.Reset();

            if (!activeSequence.HasPhases)
            {
                sequenceCompleted.OnNext(activeSequence.SequenceType);
                DisposeSequence(activeSequence);
                activeSequence = null;
                return;
            }

            MoveNextPhase();
        }

        void MoveNextPhase()
        {
            exitSubscription?.Dispose();
            exitSubscription = null;
            activePhase?.Exit();

            if (activeSequence == null)
            {
                return;
            }

            if (!activeSequence.TryMoveNext(out var nextPhase))
            {
                playableDirector?.Stop();
                sequenceCompleted.OnNext(activeSequence.SequenceType);
                DisposeSequence(activeSequence);
                activeSequence = null;
                activePhase = null;
                return;
            }

            activePhase = nextPhase;
            ApplyTimeline(activePhase);
            activePhase.Enter(playableDirector);
            phaseStarted.OnNext(activePhase);

            exitSubscription = activePhase.OnExitPhase.Subscribe(_ => MoveNextPhase());
        }

        void ApplyTimeline(BattlePhaseModelBase phase)
        {
            if (!playableDirector)
            {
                Debug.LogWarning("PlayableDirector is not assigned.", this);
                return;
            }

            var timeline = phase.ResolveTimeline();
            if (!timeline)
            {
                Debug.LogWarning($"Phase {phase.PhaseId} does not have a timeline asset.", this);
                return;
            }

            playableDirector.playableAsset = timeline;
            playableDirector.time = 0;
            playableDirector.Evaluate();
            playableDirector.extrapolationMode = DirectorWrapMode.Loop;

            if (bindingMap)
            {
                bindingMap.ApplyBindings(playableDirector, timeline);
            }

            playableDirector.Play();
        }

        public void Stop()
        {
            exitSubscription?.Dispose();
            exitSubscription = null;

            if (playableDirector)
            {
                playableDirector.Stop();
            }

            activePhase?.Exit();
            activePhase = null;

            if (activeSequence != null)
            {
                DisposeSequence(activeSequence);
                activeSequence = null;
            }
        }

        void DisposeSequence(BattlePhaseSequenceModel sequence)
        {
            if (sequence == null)
            {
                return;
            }

            foreach (var phase in sequence.Phases)
            {
                phase.Dispose();
            }
        }

        void OnDestroy()
        {
            Stop();
            phaseStarted.Dispose();
            sequenceCompleted.Dispose();
        }
    }
}
