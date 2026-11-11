using System;
using UniRx;
using UnityEngine;
using Project.Scenes.Battle.Scripts.Model;
using Project.Scripts.Model;

namespace Project.Scenes.Battle.Scripts.Presenter
{
    public class BattlePhaseStateMachine : MonoBehaviour
    {
        [SerializeField] EnemySpawnReceiver enemySpawnReceiver;
        [SerializeField] BulletClearReceiver bulletClearReceiver;

        readonly Subject<BattlePhaseModelBase> phaseStarted = new();
        readonly Subject<BattleSituation> sequenceCompleted = new();

        BattleSequenceModel activeSequence;
        BattlePhaseModelBase activePhase;
        // 直前に終了したフェーズ。そのフェーズのOnExitPhase通知の最中に破棄しないよう、次の遷移まで破棄を遅らせる
        BattlePhaseModelBase exitedPhase;
        IDisposable exitSubscription;
        PhaseActionRunner actionRunner;

        public IObservable<BattlePhaseModelBase> OnPhaseStarted => phaseStarted;
        public IObservable<BattleSituation> OnSequenceCompleted => sequenceCompleted;

        void Awake()
        {
            // 未設定ならシーンから探す（シーン上の配置は変えずに済むようにする）
            if (!enemySpawnReceiver) enemySpawnReceiver = FindFirstObjectByType<EnemySpawnReceiver>();
            if (!bulletClearReceiver) bulletClearReceiver = FindFirstObjectByType<BulletClearReceiver>();

            actionRunner = new PhaseActionRunner(
                action =>
                {
                    if (enemySpawnReceiver) enemySpawnReceiver.Spawn(action);
                },
                () =>
                {
                    if (bulletClearReceiver) bulletClearReceiver.ClearAll();
                });
        }

        void Update()
        {
            BattleWind.Tick(Time.deltaTime);
            actionRunner?.Tick(Time.deltaTime);
        }

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

            if (exitedPhase != null)
            {
                activeSequence.ReleasePhase(exitedPhase);
            }
            exitedPhase = activePhase;

            var nextPhase = activeSequence.MoveNext();
            if (nextPhase == null)
            {
                BattleWind.Reset();
                actionRunner?.Stop();
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
            actionRunner?.Stop();
            nextPhase.Enter();

            // Enter() 中に即完了して MoveNextPhase が再帰済みの場合、activePhase は既に
            // 後続フェーズへ進んでいるため、このフェーズ分のアクション開始と開始通知は行わない。
            if (activePhase == nextPhase)
            {
                actionRunner?.Start(nextPhase.ActionAsset);
                phaseStarted.OnNext(nextPhase);
            }
        }

        public void Stop(BattleSequenceModel sequenceToKeep = null)
        {
            BattleWind.Reset();
            Debug.Log($"[BattlePhaseStateMachine] Stop called, activeSequence: {activeSequence?.Situation}", this);
            exitSubscription?.Dispose();
            exitSubscription = null;

            actionRunner?.Stop();

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

            // 先に enumerator を破棄して Loop の終了条件の監視を止め、破棄済みフェーズへの interrupt を防ぐ
            sequence.Dispose();

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
