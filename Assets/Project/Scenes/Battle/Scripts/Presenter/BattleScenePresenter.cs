using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UniRx;
using UnityEngine;
using UnityEngine.SceneManagement;
using Project.Scenes.Battle.Scripts.Model;
using Project.Scripts.Model;
using Project.Scripts.Repository.ModelRepository;
using Project.Scenes.Battle.Scripts.Repository.ModelRepository;
using Project.Scenes.Battle.Scripts.Presenter.Entity;
using Project.Scenes.Scenario.Scripts.Repository.ModelRepository;
using Project.Scripts.Extensions;
using Project.Scripts.Extensions.Message;
using Project.Scripts.Presenter;

namespace Project.Scenes.Battle.Scripts.Presenter
{
    public class BattleScenePresenter : MonoPresenter
    {
        [SerializeField] BattlePhaseStateMachine phaseStateMachine;
        [SerializeField] EnemyTracker enemyTracker;
        [SerializeField] BackgroundPresenter backgroundPresenter;
        [SerializeField] BulletClearReceiver bulletClearReceiver;

        BattleSequenceModelRepository sequenceModelRepository;
        readonly Subject<Unit> battleCompleted = new();
        readonly CompositeDisposable disposables = new();

        StageModel stageModel;
        BattleSequenceModel waySequence;
        BattleSequenceModel bossSequence;
        Action pendingScenarioCallback;
        PlayerEntityPresenter playerPresenter;
        BossEntityPresenter bossPresenter;
        CompositeDisposable bossDisposables = new();
        bool isSceneLoadedHandlerRegistered = false;

        IDisposable sceneNavigationSubscription;

        public IObservable<Unit> OnBattleCompleted => battleCompleted;

        bool isBattleStarted;
        bool hasResumedPlayerAnimationOnBoss;
        bool hasShownTutorial;
        CancellationTokenSource sequenceTransitionCts;
        IDisposable scenarioCompletedSubscription;
        IDisposable tutorialClosedSubscription;

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

            SubscribeToPauseInput();

            // 1面道中の初回開始時はチュートリアルモーダルを表示し、閉じてからシーケンスを開始する
            if (ShouldShowTutorial())
            {
                ShowTutorialThenStart();
            }
            else
            {
                InitializeAndStart();
            }
        }

        bool ShouldShowTutorial()
        {
            if (hasShownTutorial) return false;

            var runtimeModel = RuntimeModelRepository.Get();
            var isStage1 = runtimeModel.CurrentStageType.AsInt() == 1;
            var isWay = runtimeModel.CurrentSituation == BattleSituation.Way;
            if (!isStage1 || !isWay) return false;

            return globalScenePresenter?.TutorialModalPresenter != null;
        }

        void ShowTutorialThenStart()
        {
            hasShownTutorial = true;

            playerPresenter = FindFirstObjectByType<PlayerEntityPresenter>();
            playerPresenter?.UnsubscribeFromAttackInput();

            var tutorialModal = globalScenePresenter.TutorialModalPresenter;

            tutorialClosedSubscription?.Dispose();
            tutorialClosedSubscription = tutorialModal.OnClosed
                                                      .Take(1)
                                                      .Subscribe(_ => InitializeAndStart());

            // 初めて1面に入ったときのみスキップ待ちを有効にする
            var userModel = UserModelRepository.Instance.Get();
            var isFirstEntry = !userModel.HasEnteredStage1;
            userModel.MarkEnteredStage1();

            tutorialModal.Open(isFirstEntry);

            soundManager?.PlayBGMAsync(SceneType.Global, BgmType.Tutorial).Forget();
        }

        void SubscribeToPauseInput()
        {
            MessageBroker.Default.Receive<PlayerPauseMessage>()
                         .Subscribe(_ => TogglePause())
                         .AddTo(disposables);
        }

        void TogglePause()
        {
            // ゲームオーバー表示中はポーズ操作を無視
            if (globalScenePresenter?.GameOverModalPresenter != null
                && globalScenePresenter.GameOverModalPresenter.IsOpen)
            {
                return;
            }

            // DemoClear表示中はポーズ操作を無効化（リトライによる次ステージ誤発火防止）
            var demoClearScene = SceneManager.GetSceneByName(SceneRouterModel.DemoClear);
            if (demoClearScene.IsValid() && demoClearScene.isLoaded)
            {
                return;
            }

            var pauseModal = globalScenePresenter?.PauseModalPresenter;
            if (pauseModal == null) return;

            // オプション画面が開いている場合はオプションだけ閉じる
            var optionModal = globalScenePresenter?.OptionModalPresenter;
            if (optionModal != null && optionModal.IsOpen)
            {
                optionModal.Close();
                return;
            }

            if (pauseModal.IsOpen)
            {
                pauseModal.Close();
            }
            else
            {
                pauseModal.Open();
            }
        }

        void SubscribeToPlayerDeath()
        {
            playerPresenter.OnDeathSequenceCompleted
                           .Subscribe(_ =>
                           {
                               var gameOverModal = globalScenePresenter?.GameOverModalPresenter;
                               gameOverModal?.Open();
                           })
                           .AddTo(disposables);

            var gameOverModalForRetry = globalScenePresenter?.GameOverModalPresenter;
            if (gameOverModalForRetry != null)
            {
                gameOverModalForRetry.OnRetryRequested
                                     .Subscribe(_ => Retry())
                                     .AddTo(disposables);
            }

            var pauseModalForRetry = globalScenePresenter?.PauseModalPresenter;
            if (pauseModalForRetry != null)
            {
                pauseModalForRetry.OnRetryRequested
                                  .Subscribe(_ => Retry())
                                  .AddTo(disposables);
                pauseModalForRetry.OnTitleRequested
                                  .Subscribe(_ => ReturnToTitle())
                                  .AddTo(disposables);
            }
        }

        void Retry()
        {
            phaseStateMachine.Stop();

            // シナリオ遷移待ち中のDelayや、シーンロード待ちハンドラ・購読をすべて解除
            sequenceTransitionCts?.Cancel();
            sequenceTransitionCts?.Dispose();
            sequenceTransitionCts = null;

            if (isSceneLoadedHandlerRegistered)
            {
                SceneManager.sceneLoaded -= OnScenarioSceneLoaded;
                isSceneLoadedHandlerRegistered = false;
            }

            scenarioCompletedSubscription?.Dispose();
            scenarioCompletedSubscription = null;

            bossDisposables.Dispose();
            bossDisposables = new CompositeDisposable();
            phaseStateMachine.SetTimelineResolver(null);

            if (bossPresenter != null)
            {
                Destroy(bossPresenter.gameObject);
                bossPresenter = null;
            }

            if (bulletClearReceiver != null)
            {
                bulletClearReceiver.ClearAllBullets();
                bulletClearReceiver.ClearAllEnemies();
            }

            var scenarioScene = SceneManager.GetSceneByName(SceneRouterModel.Scenario);
            if (scenarioScene.IsValid() && scenarioScene.isLoaded)
            {
                SceneManager.UnloadSceneAsync(scenarioScene);
            }
            ScenarioModelRepository.Instance.Refresh();

            pendingScenarioCallback = null;

            playerPresenter?.Retry();
            playerPresenter?.SetColliderActive(true);
            playerPresenter?.SubscribeToAttackInput();


            hasResumedPlayerAnimationOnBoss = false;

            var startSituation = RuntimeModelRepository.Get().CurrentSituation;
            if (startSituation == BattleSituation.Boss && bossSequence != null)
            {
                backgroundPresenter?.ResetScroll(false);
                SpawnBoss();
                PlayBgmForSituation(BattleSituation.Boss);
                phaseStateMachine.PlaySequence(bossSequence);
            }
            else if (waySequence != null)
            {
                backgroundPresenter?.ResetScroll(true);
                PlayBgmForSituation(BattleSituation.Way);
                phaseStateMachine.PlaySequence(waySequence);
            }
        }

        async void ReturnToTitle()
        {
            phaseStateMachine.Stop();

            sequenceTransitionCts?.Cancel();
            sequenceTransitionCts?.Dispose();
            sequenceTransitionCts = null;

            if (isSceneLoadedHandlerRegistered)
            {
                SceneManager.sceneLoaded -= OnScenarioSceneLoaded;
                isSceneLoadedHandlerRegistered = false;
            }

            scenarioCompletedSubscription?.Dispose();
            scenarioCompletedSubscription = null;

            var scenarioScene = SceneManager.GetSceneByName(SceneRouterModel.Scenario);
            if (scenarioScene.IsValid() && scenarioScene.isLoaded)
            {
                await SceneManager.UnloadSceneAsync(scenarioScene).ToUniTask();
            }
            ScenarioModelRepository.Instance.Refresh();

            var battleSceneName = SceneManager.GetSceneByName(SceneRouterModel.Battle).name;
            await SceneManager.LoadSceneAsync(SceneRouterModel.Title, LoadSceneMode.Additive).ToUniTask();
            SceneManager.SetActiveScene(SceneManager.GetSceneByName(SceneRouterModel.Title));
            SceneManager.UnloadSceneAsync(battleSceneName).ToUniTask().Forget();
        }

        public void InitializeAndStart()
        {
            stageModel = ResolveStageModel();
            if (stageModel == null)
            {
                Debug.LogError("StageModel could not be resolved.", this);
                return;
            }

            // PlayerEntityPresenterを取得（チュートリアル経由の場合は取得済み）
            if (playerPresenter == null) playerPresenter = FindFirstObjectByType<PlayerEntityPresenter>();
            if (playerPresenter == null)
            {
                Debug.LogWarning("[BattleScenePresenter] PlayerEntityPresenter not found in scene.", this);
            }
            else
            {
                SubscribeToPlayerDeath();
            }

            backgroundPresenter?.Initialize();
            playerPresenter?.Initialize();
            playerPresenter?.SubscribeToAttackInput();

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
            sequenceTransitionCts?.Cancel();
            sequenceTransitionCts?.Dispose();
            sequenceTransitionCts = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);
            var token = sequenceTransitionCts.Token;

            try
            {
                await UniTask.Delay(TimeSpan.FromSeconds(2), cancellationToken: token);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            if (token.IsCancellationRequested) return;

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

            // シナリオ中は攻撃を禁止 + プレイヤーの当たり判定を無効化（被弾でゲームオーバー誤発火を防ぐ）
            playerPresenter?.UnsubscribeFromAttackInput();
            playerPresenter?.SetColliderActive(false);
            // チャージ中のままシナリオへ突入した場合に備えてチャージ状態をリセット
            playerPresenter?.CancelCharge();

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
                // Retryで途中解除できるよう、購読を保持しておく
                scenarioCompletedSubscription?.Dispose();
                scenarioCompletedSubscription = scenarioPresenter.OnScenarioCompleted
                                 .Take(1) // 1回だけ実行
                                 .Subscribe(_ => OnScenarioCompleted());
            }
            else
            {
                Debug.LogWarning("ScenarioScenePresenter not found in the loaded scene.", this);
            }
        }

        void OnScenarioCompleted()
        {
            Debug.Log("[BattleScenePresenter] Scenario completed, invoking callback.", this);

            // シナリオ完了後は攻撃と当たり判定を再開
            playerPresenter?.SetColliderActive(true);
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

            playerPresenter?.FreezeRunAnimation();
            hasResumedPlayerAnimationOnBoss = false;

            phaseStateMachine.SetTimelineResolver(phase =>
            {
                if (ShouldUseStrongAttack(phase))
                {
                    return phase.ResolveTimelineStrong();
                }
                return phase.ResolveTimeline();
            });
            phaseStateMachine.OnPhaseStarted
                             .Subscribe(phase =>
                             {
                                 if (!hasResumedPlayerAnimationOnBoss)
                                 {
                                     hasResumedPlayerAnimationOnBoss = true;
                                     playerPresenter?.UnfreezeRunAnimation();
                                 }

                                 bool useStrong = ShouldUseStrongAttack(phase);
                                 if (useStrong)
                                 {
                                     bossPresenter.Model.EnterStrongMode();
                                 }
                                 var builder = useStrong ? phase.BuilderStrong : phase.Builder;
                                 bossPresenter.OnPhaseStarted(phase, builder);
                             })
                             .AddTo(bossDisposables);

            bossPresenter.OnDeath
                         .Take(1)
                         .Subscribe(_ => phaseStateMachine.Stop())
                         .AddTo(bossDisposables);

            bossPresenter.OnDeathSequenceCompleted
                         .Take(1)
                         .Subscribe(_ => HandleSequenceCompleted(BattleSituation.Boss))
                         .AddTo(bossDisposables);

            bossPresenter.PlayEntranceMovement(bossSequence.BossEntranceMovement);

            backgroundPresenter?.StartDeceleration();

            Debug.Log($"[BattleScenePresenter] Boss spawned at {bossSequence.BossSpawnPosition}", this);
        }

        bool ShouldUseStrongAttack(BattlePhaseModelBase phase)
        {
            if (phase.BuilderStrong == null || bossPresenter?.Model == null) return false;
            return bossPresenter.Model.ShouldUseStrongAttack;
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
                backgroundPresenter?.ResetScroll();
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

            scenarioCompletedSubscription?.Dispose();
            scenarioCompletedSubscription = null;
            sequenceTransitionCts?.Cancel();
            sequenceTransitionCts?.Dispose();
            sequenceTransitionCts = null;

            // チュートリアルモーダルを閉じる前にシーンが破棄された場合に備えて後始末する
            tutorialClosedSubscription?.Dispose();
            tutorialClosedSubscription = null;
            var tutorialModal = globalScenePresenter?.TutorialModalPresenter;
            if (tutorialModal != null && tutorialModal.IsOpen)
            {
                tutorialModal.Close();
            }

            disposables.Dispose();
            bossDisposables.Dispose();
            battleCompleted.Dispose();
            phaseStateMachine?.Stop();
        }
    }
}
