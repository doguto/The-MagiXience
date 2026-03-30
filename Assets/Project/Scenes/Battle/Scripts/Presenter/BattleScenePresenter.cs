using System;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UniRx;
using UnityEngine;
using UnityEngine.SceneManagement;
using Project.Scenes.Battle.Scripts.Model;
using Project.Scripts.Model;
using Project.Scripts.Repository.ModelRepository;
using Project.Scenes.Battle.Scripts.Repository.ModelRepository;
using Project.Scenes.Battle.Scripts.Presenter.Entity;
using Project.Scripts.Extensions;
using Project.Scripts.Extensions.Message;
using Project.Scripts.Presenter;

namespace Project.Scenes.Battle.Scripts.Presenter
{
    public class BattleScenePresenter : MonoPresenter
    {
        [SerializeField] BattlePhaseStateMachine phaseStateMachine;
        [SerializeField] EnemyTracker enemyTracker;

        BattleSequenceModelRepository sequenceModelRepository;
        readonly Subject<Unit> battleCompleted = new();
        readonly CompositeDisposable disposables = new();

        StageModel stageModel;
        BattleSequenceModel waySequence;
        BattleSequenceModel bossSequence;
        Action pendingScenarioCallback;
        PlayerEntityPresenter playerPresenter;
        BossEntityPresenter bossPresenter;
        bool isSceneLoadedHandlerRegistered = false;

        IDisposable sceneNavigationSubscription;

        public IObservable<Unit> OnBattleCompleted => battleCompleted;

        bool isBattleStarted;

        void Awake()
        {
            sequenceModelRepository = new BattleSequenceModelRepository(enemyTracker);
            sequenceModelRepository.SetBossModelProvider(() => bossPresenter != null ? bossPresenter.Model : null);
            sequenceModelRepository.SetBgmAudioSourceProvider(() => soundManager != null ? soundManager.BgmAudioSource : null);
            phaseStateMachine ??= GetComponent<BattlePhaseStateMachine>();
            ScreenBoundsCache.Initialize(gameObject.scene);

            sceneNavigationSubscription = MessageBroker.Default.Receive<SceneNavigationMessage>().Subscribe(message =>
            {
                if (message.SceneName != SceneRouterModel.Battle) return;
                if (message.State != SceneNavigationState.Completed) return;

                Debug.Log("[BattleScenePresenter] Start Event", this);
                sceneNavigationSubscription?.Dispose();
                StartBattle();
            });
        }

        void StartBattle()
        {
            base.Start();

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

            // PlayerEntityPresenterを取得
            playerPresenter = FindFirstObjectByType<PlayerEntityPresenter>();
            if (playerPresenter == null)
            {
                Debug.LogWarning("[BattleScenePresenter] PlayerEntityPresenter not found in scene.", this);
            }

            waySequence = LoadSequence(stageModel.WaySequenceAddress);
            bossSequence = LoadSequence(stageModel.BossSequenceAddress);

            var startSituation = RuntimeModelRepository.Instance.Get().CurrentSituation;
            if (startSituation == BattleSituation.Boss && bossSequence != null)
            {
                SpawnBoss();
                PlayBgmForSituation(BattleSituation.Boss);
                phaseStateMachine.PlaySequence(bossSequence);
            }
            else if (waySequence != null)
            {
                PlayBgmForSituation(BattleSituation.Way);
                phaseStateMachine.PlaySequence(waySequence);
            }
            else
            {
                Debug.LogError("No battle sequences are configured for this stage.", this);
            }
        }

        public void PlayBossBgm()
        {
            PlayBgmForSituation(BattleSituation.Boss);
        }

        void PlayBgmForSituation(BattleSituation situation)
        {
            if (soundManager == null) return;

            var stageNumber = stageModel.StageNumber;
            // SceneType.Stage1=3, Stage2=4, ... なので stageNumber+2 で変換
            var sceneType = (SceneType)(stageNumber + 2);
            var bgmType = situation == BattleSituation.Boss ? BgmType.BattleBoss : BgmType.BattleWay;
            soundManager.PlayBGMAsync(sceneType, bgmType).Forget();
        }

        StageModel ResolveStageModel()
        {
            var runtimeModel = RuntimeModelRepository.Instance.Get();
            var stageNumber = runtimeModel.CurrentStageType.AsInt() < 1 ? 1 : runtimeModel.CurrentStageType.AsInt();

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
                return sequenceModelRepository.Load(address);
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to load phase sequence from {address}: {e.Message}", this);
                return null;
            }
        }

        async void HandleSequenceCompleted(BattleSituation situation)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(2), cancellationToken: destroyCancellationToken);

            if (situation == BattleSituation.Way)
            {
                TransitionToScenario(StartBossSequence);
            }
            else
            {
                TransitionToScenario(DemoClear);
            }
        }

        void TransitionToScenario(Action onCompleteOrSkip)
        {
            // ScenarioIdは不要、ScenarioModelRepositoryがRuntimeModelから自動決定
            Debug.Log($"[BattleScenePresenter] TransitionToScenario called", this);

            // シナリオ中は攻撃を禁止
            playerPresenter?.UnsubscribeFromAttackInput();

            // シナリオ完了後のコールバックを保存
            pendingScenarioCallback = onCompleteOrSkip;

            // シーンロード完了を待ってからイベントを購読
            if (!isSceneLoadedHandlerRegistered)
            {
                SceneManager.sceneLoaded += OnScenarioSceneLoaded;
                isSceneLoadedHandlerRegistered = true;
            }

            SceneManager.LoadScene(SceneRouterModel.Scenario, LoadSceneMode.Additive);
        }

        void OnScenarioSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != SceneRouterModel.Scenario) return;

            Debug.Log($"[BattleScenePresenter] Scenario scene loaded", this);

            // ハンドラを解除
            if (isSceneLoadedHandlerRegistered)
            {
                SceneManager.sceneLoaded -= OnScenarioSceneLoaded;
                isSceneLoadedHandlerRegistered = false;
            }

            // ScenarioScenePresenterを見つけてイベントを購読
            var scenarioPresenter = FindFirstObjectByType<Project.Scenes.Scenario.Scripts.Presenter.ScenarioScenePresenter>();
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

            // シナリオ完了後は攻撃を再開
            // temp: デモ版ではボス戦終了後に攻撃できない
            if (RuntimeModelRepository.Instance.Get().CurrentSituation == BattleSituation.Boss)
            {
                playerPresenter?.SubscribeToAttackInput();
            }

            pendingScenarioCallback?.Invoke();
            pendingScenarioCallback = null;
        }

        public void SpawnBoss()
        {
            if (bossPresenter != null)
            {
                Debug.LogWarning("[BattleScenePresenter] Boss already spawned.", this);
                return;
            }

            if (bossSequence == null || bossSequence.BossPrefab == null)
            {
                Debug.LogWarning("[BattleScenePresenter] BossPrefab is not configured in boss sequence.", this);
                return;
            }

            var instance = Instantiate(bossSequence.BossPrefab, bossSequence.BossSpawnPosition, Quaternion.identity);
            bossPresenter = instance.GetComponent<BossEntityPresenter>();

            if (bossPresenter == null)
            {
                Debug.LogError("[BattleScenePresenter] Spawned boss prefab has no BossEntityPresenter.", this);
                return;
            }

            phaseStateMachine.OnPhaseStarted
                             .Subscribe(phase => bossPresenter.OnPhaseStarted(phase))
                             .AddTo(disposables);

            PlayEntranceMovement(instance.transform);

            Debug.Log($"[BattleScenePresenter] Boss spawned at {bossSequence.BossSpawnPosition}", this);
        }

        void PlayEntranceMovement(Transform bossTransform)
        {
            var steps = bossSequence.BossEntranceMovement;
            if (steps == null || steps.Count == 0) return;

            var sequence = DOTween.Sequence();
            foreach (var step in steps)
            {
                if (step == null) continue;
                sequence.Append(step.Play(bossTransform, Vector2.zero, bossTransform.GetComponent<Animator>()));
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

        async void DemoClear()
        {
            stageModel?.Clear();

            await SceneManager.LoadSceneAsync(SceneRouterModel.DemoClear, LoadSceneMode.Additive).ToUniTask();

            SceneManager.SetActiveScene(SceneManager.GetSceneByName(SceneRouterModel.DemoClear));
        }

        void CompleteStage()
        {
            stageModel?.Clear();

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
            bossSequence = LoadSequence(stageModel.BossSequenceAddress);

            // 道中シーケンスを開始
            if (waySequence != null)
            {
                Debug.Log($"[BattleScenePresenter] Starting next stage {stageModel.StageNumber}", this);
                PlayBgmForSituation(BattleSituation.Way);
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
            // 登録されているハンドラを確実に解除
            if (isSceneLoadedHandlerRegistered)
            {
                SceneManager.sceneLoaded -= OnScenarioSceneLoaded;
                isSceneLoadedHandlerRegistered = false;
            }

            disposables.Dispose();
            battleCompleted.Dispose();
            phaseStateMachine?.Stop();
        }
    }
}
