using System;
using UniRx;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
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
        // 直前に終了したフェーズ。そのフェーズのOnExitPhase通知の最中に破棄しないよう、次の遷移まで破棄を遅らせる
        BattlePhaseModelBase exitedPhase;
        IDisposable exitSubscription;
        Func<BattlePhaseModelBase, TimelineAsset> timelineResolver;

        public IObservable<BattlePhaseModelBase> OnPhaseStarted => phaseStarted;
        public IObservable<BattleSituation> OnSequenceCompleted => sequenceCompleted;

        public void SetTimelineResolver(Func<BattlePhaseModelBase, TimelineAsset> resolver)
            => timelineResolver = resolver;

        void Update() => BattleWind.Tick(Time.deltaTime);

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
            BattleWind.Begin(sequence.Wind);

            if (!activeSequence.HasPhases)
            {
                BattleWind.Reset();
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

            // Directorがまだ再生中のTimelineAssetを持つフェーズは破棄しない(次フェーズにTimelineが無い場合など)
            if (exitedPhase != null && !(playableDirector && exitedPhase.OwnsTimeline(playableDirector.playableAsset)))
            {
                activeSequence.ReleasePhase(exitedPhase);
            }
            exitedPhase = activePhase;

            var nextPhase = activeSequence.MoveNext();
            if (nextPhase == null)
            {
                BattleWind.Reset();
                playableDirector?.Stop();
                sequenceCompleted.OnNext(activeSequence.Situation);
                DisposeSequence(activeSequence);
                activeSequence = null;
                activePhase = null;
                return;
            }

            activePhase = nextPhase;

            // Enter() 内で ExitCondition が即座に満たされ CompletePhase が同期発火するケースがあるため、
            // 購読を Enter() より先に済ませておく。後から購読すると OnExitPhase の通知を取りこぼし、
            // シーケンスがそのフェーズで止まったままになる。
            exitSubscription = nextPhase.OnExitPhase.Subscribe(_ => MoveNextPhase());
            ApplyTimeline(nextPhase);
            nextPhase.Enter(playableDirector);

            // Enter() 中に即完了して MoveNextPhase が再帰済みの場合、activePhase は既に
            // 後続フェーズへ進んでいるため、このフェーズ分の開始通知は出さない。
            if (activePhase == nextPhase)
            {
                phaseStarted.OnNext(nextPhase);
            }
        }

        void ApplyTimeline(BattlePhaseModelBase phase)
        {
            if (!playableDirector)
            {
                Debug.LogWarning("PlayableDirector is not assigned.", this);
                return;
            }

            var timeline = timelineResolver?.Invoke(phase) ?? phase.ResolveTimeline();
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
            BattleWind.Reset();
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

            foreach (var phase in sequence.AllCreatedPhases)
            {
                phase.Dispose();
            }

            if (sequence == activeSequence)
            {
                exitedPhase = null;
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
