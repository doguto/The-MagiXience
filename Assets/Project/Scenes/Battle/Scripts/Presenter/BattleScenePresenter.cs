using System;
using UniRx;
using UnityEngine;
using Project.Scenes.Battle.Scripts.Model;
using Project.Scripts.Model;
using Project.Scripts.Repository.ModelRepository;

namespace Project.Scenes.Battle.Scripts.Presenter
{
    public class BattleScenePresenter : MonoBehaviour
    {
        [SerializeField] BattlePhaseStateMachine phaseStateMachine;
        [SerializeField, Min(1)] int fallbackStageNumber = 1;
        [SerializeField] bool playOnStart = true;
        [SerializeField] bool autoCompleteScenarioRequests = true;

        readonly BattlePhaseSequenceRepository sequenceRepository = new();
        readonly Subject<ScenarioTransitionRequest> scenarioRequests = new();
        readonly Subject<Unit> battleCompleted = new();
        readonly CompositeDisposable disposables = new();

        StageModel stageModel;
        BattlePhaseSequenceModel waySequence;
        BattlePhaseSequenceModel bossSequence;
        ScenarioTransitionTiming? pendingScenario;

        public IObservable<ScenarioTransitionRequest> OnScenarioRequested => scenarioRequests;
        public IObservable<Unit> OnBattleCompleted => battleCompleted;

        void Awake()
        {
            phaseStateMachine ??= GetComponent<BattlePhaseStateMachine>();
        }

        void Start()
        {
            if (!phaseStateMachine)
            {
                Debug.LogError("BattlePhaseStateMachine is not assigned.", this);
                return;
            }

            phaseStateMachine.OnSequenceCompleted
                .Subscribe(HandleSequenceCompleted)
                .AddTo(disposables);

            if (playOnStart)
            {
                InitializeAndStart();
            }
        }

        public void InitializeAndStart()
        {
            stageModel = ResolveStageModel();
            if (stageModel == null)
            {
                Debug.LogError("StageModel could not be resolved.", this);
                return;
            }

            waySequence = LoadSequence(stageModel.WaySequenceAddress);
            bossSequence = LoadSequence(stageModel.BossSequenceAddress);

            if (waySequence != null)
            {
                phaseStateMachine.PlaySequence(waySequence);
            }
            else if (bossSequence != null)
            {
                Debug.LogWarning("Way sequence is missing. Starting from boss sequence.", this);
                phaseStateMachine.PlaySequence(bossSequence);
            }
            else
            {
                Debug.LogWarning("No battle sequences are configured for this stage.", this);
            }
        }

        StageModel ResolveStageModel()
        {
            var runtimeModel = RuntimeModelRepository.Instance.Get();
            var stageNumber = runtimeModel.CurrentStageNumber >= 0 ? runtimeModel.CurrentStageNumber : fallbackStageNumber;

            try
            {
                var model = StageModelRepository.Instance.GetByStageNumber(stageNumber);
                model.Start();
                return model;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to resolve StageModel for stage {stageNumber}: {e.Message}", this);
                return null;
            }
        }

        BattlePhaseSequenceModel LoadSequence(string address)
        {
            if (string.IsNullOrEmpty(address))
            {
                return null;
            }

            try
            {
                return sequenceRepository.Load(address);
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to load phase sequence from {address}: {e.Message}", this);
                return null;
            }
        }

        void HandleSequenceCompleted(BattleSequenceType sequenceType)
        {
            if (sequenceType == BattleSequenceType.Way)
            {
                if (!RequestScenario(stageModel.ScenarioIdWayToBoss, ScenarioTransitionTiming.WayToBoss))
                {
                    StartBossSequence();
                }
            }
            else
            {
                if (!RequestScenario(stageModel.ScenarioIdBossToNext, ScenarioTransitionTiming.BossToNextStage))
                {
                    CompleteStage();
                }
            }
        }

        bool RequestScenario(string scenarioId, ScenarioTransitionTiming timing)
        {
            Debug.Log($"Requesting scenario transition: {scenarioId} at {timing}", this);
            if (string.IsNullOrEmpty(scenarioId))
            {
                return false;
            }

            pendingScenario = timing;
            var request = new ScenarioTransitionRequest(stageModel.StageNumber, scenarioId, timing);
            scenarioRequests.OnNext(request);

            if (autoCompleteScenarioRequests)
            {
                CompleteScenarioTransition(timing);
            }

            return true;
        }

        public void CompleteScenarioTransition(ScenarioTransitionTiming timing)
        {
            if (pendingScenario != timing)
            {
                return;
            }

            pendingScenario = null;

            if (timing == ScenarioTransitionTiming.WayToBoss)
            {
                StartBossSequence();
            }
            else
            {
                CompleteStage();
            }
        }

        void StartBossSequence()
        {
            if (bossSequence == null)
            {
                Debug.LogError("Boss sequence is not configured.", this);
                return;
            }

            phaseStateMachine.PlaySequence(bossSequence);
        }

        void CompleteStage()
        {
            try
            {
                stageModel?.Clear();
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to mark stage as cleared: {e.Message}", this);
            }

            battleCompleted.OnNext(Unit.Default);
        }

        void OnDestroy()
        {
            disposables.Dispose();
            scenarioRequests.Dispose();
            battleCompleted.Dispose();
            phaseStateMachine?.Stop();
        }
    }
}
