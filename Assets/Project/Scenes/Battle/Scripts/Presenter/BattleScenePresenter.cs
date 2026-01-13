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
        [SerializeField, Min(1)] int initialStageNumber = 1;

        readonly BattleSequenceRepository sequenceRepository = new();
        readonly Subject<Unit> battleCompleted = new();
        readonly CompositeDisposable disposables = new();

        StageModel stageModel;
        BattleSequenceModel waySequence;
        BattleSequenceModel bossSequence;
        System.Action pendingScenarioCallback;

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

            InitializeAndStart();
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
            var stageNumber = runtimeModel.CurrentStageNumber < initialStageNumber ? initialStageNumber : runtimeModel.CurrentStageNumber;

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

        BattleSequenceModel LoadSequence(string address)
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
                TransitionToScenario(StartBossSequence);
            }
            else
            {
                TransitionToScenario(CompleteStage);
            }
        }

        void TransitionToScenario(System.Action onCompleteOrSkip)
        {
            // ScenarioIdは不要、ScenarioModelRepositoryがRuntimeModelから自動決定
            Debug.Log($"[BattleScenePresenter] TransitionToScenario called", this);

            // シナリオ完了後のコールバックを保存
            pendingScenarioCallback = onCompleteOrSkip;

            // シーンロード完了を待ってからイベントを購読
            SceneManager.sceneLoaded += OnScenarioSceneLoaded;
            SceneManager.LoadScene(SceneRouterModel.Scenario, LoadSceneMode.Additive);
        }

        void OnScenarioSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != SceneRouterModel.Scenario) return;

            Debug.Log($"[BattleScenePresenter] Scenario scene loaded", this);
            SceneManager.sceneLoaded -= OnScenarioSceneLoaded;

            // ScenarioScenePresenterを見つけてイベントを購読
            var scenarioPresenter = FindObjectOfType<Project.Scenes.Scenario.Scripts.Presenter.ScenarioScenePresenter>();
            if (scenarioPresenter != null)
            {
                scenarioPresenter.OnScenarioCompleted
                    .Take(1) // 1回だけ実行
                    .Subscribe(_ => OnScenarioCompleted())
                    .AddTo(disposables);
            }
            else
            {
                Debug.LogWarning("ScenarioScenePresenter not found in the loaded scene.", this);
            }
        }

        void OnScenarioCompleted()
        {
            Debug.Log("[BattleScenePresenter] Scenario completed, invoking callback.", this);
            pendingScenarioCallback?.Invoke();
            pendingScenarioCallback = null;
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
                battleCompleted.OnNext(Unit.Default);
                return;
            }

            // 次のステージをロード
            var nextStageModel = ResolveStageModel();
            if (nextStageModel == null)
            {
                // 次のステージが存在しない（最終ステージクリア）
                Debug.Log("[BattleScenePresenter] All stages cleared!", this);
                battleCompleted.OnNext(Unit.Default);
                return;
            }

            // 次のステージのシーケンスをロード
            stageModel = nextStageModel;
            waySequence = LoadSequence(stageModel.WaySequenceAddress);

            // 道中シーケンスを開始
            if (waySequence != null)
            {
                Debug.Log($"[BattleScenePresenter] Starting next stage {stageModel.StageNumber}", this);
                phaseStateMachine.PlaySequence(waySequence);
            }
            else
            {
                Debug.LogError("[BattleScenePresenter] Next stage way sequence is missing.", this);
                battleCompleted.OnNext(Unit.Default);
            }
        }

        void OnDestroy()
        {
            disposables.Dispose();
            battleCompleted.Dispose();
            phaseStateMachine?.Stop();
        }
    }
}
