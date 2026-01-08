using System;
using UniRx;
using UnityEngine;
using UnityEngine.SceneManagement;
using Project.Scenes.Battle.Scripts.Model;
using Project.Scripts.Model;
using Project.Scripts.Repository.ModelRepository;
using Project.Scenes.Battle.Scripts.Repository.ModelRepository;

namespace Project.Scenes.Battle.Scripts.Presenter
{
    public class BattleScenePresenter : MonoBehaviour
    {
        [SerializeField] BattlePhaseStateMachine phaseStateMachine;
        [SerializeField, Min(1)] int fallbackStageNumber = 1;
        [SerializeField] bool playOnStart = true;

        readonly BattlePhaseSequenceRepository sequenceRepository = new();
        readonly Subject<Unit> battleCompleted = new();
        readonly CompositeDisposable disposables = new();

        StageModel stageModel;
        BattlePhaseSequenceModel waySequence;
        BattlePhaseSequenceModel bossSequence;

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
                TransitionToScenario(stageModel.ScenarioIdWayToBoss, StartBossSequence);
            }
            else
            {
                TransitionToScenario(stageModel.ScenarioIdBossToNext, CompleteStage);
            }
        }

        void TransitionToScenario(string scenarioId, System.Action onCompleteOrSkip)
        {
            if (string.IsNullOrEmpty(scenarioId))
            {
                Debug.Log("No scenario configured. Proceeding to next phase.", this);
                onCompleteOrSkip?.Invoke();
                return;
            }

            Debug.Log($"Loading scenario: {scenarioId}", this);
            SceneManager.LoadScene(scenarioId);
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
            battleCompleted.Dispose();
            phaseStateMachine?.Stop();
        }
    }
}
