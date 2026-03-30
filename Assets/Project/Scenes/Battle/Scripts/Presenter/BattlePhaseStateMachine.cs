using System;
using UniRx;
using UnityEngine;
using UnityEngine.Playables;
using Project.Scenes.Battle.Scripts.Model;
using Project.Scripts.Model;

namespace Project.Scenes.Battle.Scripts.Presenter
{
    public class BattlePhaseStateMachine : MonoBehaviour
    {
        [SerializeField] PlayableDirector playableDirector;
        [SerializeField] BattleTimelineBindingMap bindingMap;

        readonly Subject<BattlePhaseModelBase> phaseStarted = new();
        readonly Subject<BattleSituation> sequenceCompleted = new();

        BattleSequenceModel activeSequence;
        BattlePhaseModelBase activePhase;
        IDisposable exitSubscription;

        public IObservable<BattlePhaseModelBase> OnPhaseStarted => phaseStarted;
        public IObservable<BattleSituation> OnSequenceCompleted => sequenceCompleted;

        public void PlaySequence(BattleSequenceModel sequence)
        {
            if (sequence == null)
            {
                Debug.LogWarning("Sequence is null.", this);
                return;
            }

            Debug.Log($"[BattlePhaseStateMachine] PlaySequence called for {sequence.Situation}", this);
            Stop(sequenceToKeep: sequence);
            activeSequence = sequence;
            activeSequence.Reset();

            if (!activeSequence.HasPhases)
            {
                sequenceCompleted.OnNext(activeSequence.Situation);
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

            var nextPhase = activeSequence.MoveNext();
            if (nextPhase == null)
            {
                playableDirector?.Stop();
                sequenceCompleted.OnNext(activeSequence.Situation);
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
            playableDirector.extrapolationMode = DirectorWrapMode.Hold;

            if (bindingMap)
            {
                bindingMap.ApplyBindings(playableDirector, timeline);
            }

            playableDirector.Play();
        }

        public void Stop(BattleSequenceModel sequenceToKeep = null)
        {
            Debug.Log($"[BattlePhaseStateMachine] Stop called, activeSequence: {activeSequence?.Situation}", this);
            exitSubscription?.Dispose();
            exitSubscription = null;

            if (playableDirector)
            {
                playableDirector.Stop();
            }

            activePhase?.Exit();
            activePhase = null;

            if (activeSequence != null && activeSequence != sequenceToKeep)
            {
                DisposeSequence(activeSequence);
                activeSequence = null;
            }
        }

        void DisposeSequence(BattleSequenceModel sequence)
        {
            if (sequence == null)
            {
                return;
            }

            Debug.Log($"[BattlePhaseStateMachine] Disposing sequence {sequence.Situation} with {sequence.AllCreatedPhases.Count} phases", this);
            foreach (var phase in sequence.AllCreatedPhases)
            {
                Debug.Log($"[BattlePhaseStateMachine] Disposing phase {phase.PhaseId}", this);
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
