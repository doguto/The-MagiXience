using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UniRx;
using UnityEngine;
using UnityEngine.SceneManagement;
using Project.Scenes.Battle.Scripts.Model;
using Project.Scripts.Model;
using Project.Scripts.Repository.ModelRepository;
using Project.Scenes.Battle.Scripts.BehaviorTree;
using Project.Scenes.Battle.Scripts.Runtime;
using UnityEngine.AddressableAssets;
using Project.Scenes.Battle.Scripts.Presenter.Entity;
using Project.Scenes.Battle.Scripts.View;
using Project.Scenes.Scenario.Scripts.Repository.ModelRepository;
using Project.Scripts.Extensions;
using Project.Scripts.Extensions.Message;
using Project.Scripts.Presenter;
using Project.Scenes.Global.Scripts.Presenter;

namespace Project.Scenes.Battle.Scripts.Presenter
{
    public class BattleScenePresenter : MonoPresenter
    {
        [SerializeField] EnemyTracker enemyTracker;
        [SerializeField] BackgroundPresenter backgroundPresenter;
        [SerializeField] BulletClearReceiver bulletClearReceiver;
        [SerializeField] BgmNameView bgmNameView;

        [Header("Continue Settings")]
        [Tooltip("コンティニュー回数に応じたPlayerの最大HP。index=コンティニュー回数（0=初回）。回数がテーブル長を超えた場合は末尾の値を使用する。")]
        [SerializeField] int[] continueHpTable = { 100 };

        BattleClock battleClock;
        BattleFlow flow;
        readonly Subject<Unit> battleCompleted = new();
        readonly CompositeDisposable disposables = new();

        StageModel stageModel;
        BattleStageAsset wayStage;
        BattleStageAsset bossStage;
        readonly BattleBackgroundModel backgroundModel = new();
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

        // コンティニュー回数。バトルシーン内でのみ保持し、シーン遷移でリセットされる。
        int continueCount;
        CancellationTokenSource sequenceTransitionCts;
        IDisposable scenarioCompletedSubscription;
        IDisposable tutorialClosedSubscription;

        void Awake()
        {
            battleClock = GetComponent<BattleClock>() ?? gameObject.AddComponent<BattleClock>();
            flow = new BattleFlow(battleClock, enemyTracker, bulletClearReceiver,
                () => soundManager != null ? soundManager.BgmAudioSource : null,
                se => soundManager?.PlaySE(se));
            ScreenBoundsCache.Initialize(GlobalScenePresenter.OrthographicSize, GlobalScenePresenter.TargetAspect);

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
                               gameOverModal?.Open(CalculateRetryHpIncreaseRate());
                           })
                           .AddTo(disposables);

            var gameOverModalForRetry = globalScenePresenter?.GameOverModalPresenter;
            if (gameOverModalForRetry != null)
            {
                gameOverModalForRetry.OnRetryRequested
                                     .Subscribe(_ => Retry(isContinue: true))
                                     .AddTo(disposables);
                gameOverModalForRetry.OnTitleRequested
                                     .Subscribe(_ => ReturnToTitle())
                                     .AddTo(disposables);
            }

            var pauseModalForRetry = globalScenePresenter?.PauseModalPresenter;
            if (pauseModalForRetry != null)
            {
                // ポーズからのリトライはコンティニュー扱いにしない（体力増加なし）
                pauseModalForRetry.OnRetryRequested
                                  .Subscribe(_ => Retry(isContinue: false))
                                  .AddTo(disposables);
                pauseModalForRetry.OnTitleRequested
                                  .Subscribe(_ => ReturnToTitle())
                                  .AddTo(disposables);
            }
        }

        // isContinue: ゲームオーバーからのリトライ(コンティニュー)ならtrue。
        //             trueの場合のみコンティニュー回数を進め、最大HPを増加させる。
        void Retry(bool isContinue)
        {
            flow.Stop();
            playerPresenter?.CancelCharge();

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
            // コンティニュー時のみ回数を進め、テーブルに応じた最大HPを適用する（Retryによる全回復の後に上書き）
            if (isContinue) continueCount++;
            ApplyPlayerMaxHp();
            playerPresenter?.SetColliderActive(true);
            playerPresenter?.SubscribeToAttackInput();


            hasResumedPlayerAnimationOnBoss = false;

            var startSituation = RuntimeModelRepository.Get().CurrentSituation;
            if (startSituation == BattleSituation.Boss && bossStage != null)
            {
                backgroundPresenter?.ResetScroll(false);
                SpawnBoss();
                PlayBgmForSituation(BattleSituation.Boss);
                StartBossSequence();
            }
            else if (wayStage != null)
            {
                backgroundPresenter?.ResetScroll(true);
                PlayBgmForSituation(BattleSituation.Way);
                StartWaySequence();
            }
        }

        async void ReturnToTitle()
        {
            flow.Stop();
            playerPresenter?.CancelCharge();

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

            backgroundModel.SetStage(stageModel.BackgroundAddress);
            backgroundPresenter?.Initialize(backgroundModel);
            playerPresenter?.Initialize();
            ApplyPlayerMaxHp();
            playerPresenter?.SubscribeToAttackInput();

            wayStage = LoadStage(stageModel.WaySequenceAddress);
            bossStage = LoadStage(stageModel.BossSequenceAddress);

            var startSituation = RuntimeModelRepository.Instance.Get().CurrentSituation;

            if (startSituation == BattleSituation.Boss && bossStage != null)
            {
                SpawnBoss();
                PlayBgmForSituation(BattleSituation.Boss);
                StartBossSequence();
            }
            else if (wayStage != null)
            {
                PlayBgmForSituation(BattleSituation.Way);
                StartWaySequence();
            }
            else
            {
                Debug.LogError("No battle sequences are configured for this stage.", this);
            }
        }

        // 現在のコンティニュー回数に応じた最大HPをPlayerに適用する。
        void ApplyPlayerMaxHp()
        {
            if (playerPresenter == null) return;
            if (continueHpTable == null || continueHpTable.Length == 0) return;

            var index = Mathf.Clamp(continueCount, 0, continueHpTable.Length - 1);
            playerPresenter.SetMaxHp(continueHpTable[index]);
        }

        // 次回リトライ時の最大HPが、初期HP(テーブル先頭)から何%増加しているかを返す。
        int CalculateRetryHpIncreaseRate()
        {
            if (continueHpTable == null || continueHpTable.Length == 0) return 0;

            var baseHp = continueHpTable[0];
            if (baseHp <= 0) return 0;

            var nextIndex = Mathf.Clamp(continueCount + 1, 0, continueHpTable.Length - 1);
            var nextHp = continueHpTable[nextIndex];
            return Mathf.RoundToInt((nextHp - baseHp) * 100f / baseHp);
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

            // 東方風に、BGMの切り替わり時に曲名を表示する
            if (bgmNameView != null)
            {
                var bgmModel = SoundModelRepository.Instance.GetBgmModel(sceneType, bgmType);
                bgmNameView.Show(bgmModel.DisplayName);
            }
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

        static BattleStageAsset LoadStage(string address)
        {
            if (string.IsNullOrEmpty(address)) return null;

            try
            {
                return Addressables.LoadAssetAsync<BattleStageAsset>(address).WaitForCompletion();
            }
            catch (Exception e)
            {
                Debug.LogError($"[BattleScenePresenter] Failed to load battle stage. address={address} error={e.Message}");
                return null;
            }
        }

        void StartWaySequence()
        {
            flow.StartStageTree(wayStage.Tree, () => HandleSequenceCompleted(BattleSituation.Way));
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
                // 制作中のステージは道中（中ボスを含む）だけでも再生できる。
                // 本ボス未設定ならボス前シナリオへ進めず、ステージクリア扱いにもせずここで終了する。
                if (bossStage == null)
                {
                    Debug.Log("[BattleScenePresenter] 道中完了。本ボスのシーケンスは未設定のため、ボス戦への移行を停止します。", this);
                    return;
                }
                TransitionToScenario(StartBossSequence);
            }
            else
            {
                TransitionToScenario(CompleteStage);
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

            if (bossStage == null || bossStage.BossPrefab == null)
            {
                Debug.LogWarning("[BattleScenePresenter] BossPrefab is not configured in boss stage.", this);
                return;
            }

            var instance = Instantiate(bossStage.BossPrefab, bossStage.BossSpawnPosition, Quaternion.identity);
            bossPresenter = instance.GetComponent<BossEntityPresenter>();

            if (bossPresenter == null)
            {
                Debug.LogError("[BattleScenePresenter] Spawned boss prefab has no BossEntityPresenter.", this);
                return;
            }

            playerPresenter?.FreezeRunAnimation();
            hasResumedPlayerAnimationOnBoss = false;

            bossPresenter.OnDeath
                         .Take(1)
                         .Subscribe(_ => flow.World?.Services.Boss?.Stop())
                         .AddTo(bossDisposables);

            bossPresenter.OnDeathSequenceCompleted
                         .Take(1)
                         .Subscribe(_ => HandleSequenceCompleted(BattleSituation.Boss))
                         .AddTo(bossDisposables);

            flow.PlayEntrance(bossPresenter.transform, bossStage.BossEntrance);

            backgroundPresenter?.StartDeceleration();

            Debug.Log($"[BattleScenePresenter] Boss spawned at {bossStage.BossSpawnPosition}", this);
        }

        void StartBossSequence()
        {
            if (bossStage == null)
            {
                Debug.LogError("Boss stage is not configured.", this);
                return;
            }

            // シナリオ明けにプレイヤーとボスが重なっていても即被弾しないよう、少しの間ボスの当たり判定を切る
            bossPresenter?.DisableColliderOnEntry();

            if (!hasResumedPlayerAnimationOnBoss)
            {
                hasResumedPlayerAnimationOnBoss = true;
                playerPresenter?.UnfreezeRunAnimation();
            }

            if (bossPresenter != null) flow.StartBossTree(bossPresenter.gameObject, bossStage.BossTree);
            if (bossStage.Tree != null) flow.StartStageTree(bossStage.Tree, null);
        }

        // クリアしたステージの次のステージを開放する。
        void OpenNextStage(StageModel clearedStage)
        {
            if (clearedStage == null) return;

            var nextStageNumber = clearedStage.StageNumber + 1;
            try
            {
                StageModelRepository.Instance.GetByStageNumber(nextStageNumber).Open();
            }
            catch (Exception)
            {
                // 次のステージが存在しない（最終ステージクリア）場合は何もしない
            }
        }

        StageClearRank ResolveClearRank()
        {
            if (continueCount > 0) return StageClearRank.Normal;
            if (playerPresenter != null && playerPresenter.HasTakenDamage) return StageClearRank.NoContinue;
            return StageClearRank.NoDamage;
        }

        async void CompleteStage()
        {
            stageModel?.Clear(ResolveClearRank());
            OpenNextStage(stageModel);

            // 次のステージをロードせず、To be continued画面を経由してステージ選択画面へ遷移する
            flow.Stop();

            // BGMの切り替え・StageListへの遷移・Battleシーンの破棄はDemoClearScenePresenterが行う
            await SceneManager.LoadSceneAsync(SceneRouterModel.DemoClear, LoadSceneMode.Additive).ToUniTask();
            SceneManager.SetActiveScene(SceneManager.GetSceneByName(SceneRouterModel.DemoClear));
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
            backgroundModel.Dispose();
            flow?.Stop();
        }
    }
}
