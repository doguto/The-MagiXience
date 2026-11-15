using System;
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
using Project.Scenes.Scenario.Scripts.Model;
using Project.Scenes.Scenario.Scripts.Presenter;
using Project.Scenes.Scenario.Scripts.Repository.ModelRepository;
using Project.Scripts.Extensions;
using Project.Scripts.Extensions.Message;
using Project.Scripts.Presenter;
using Project.Scenes.Global.Scripts.Presenter;

namespace Project.Scenes.Battle.Scripts.Presenter
{
    // ステージのツリーを動かし、ツリーから届く出来事 (Section の開始・ボスの出現・会話・BGM) にシーン側の演出で応える
    public class BattleScenePresenter : MonoPresenter, IScenarioService
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
        BattleStageAsset stage;
        readonly BattleBackgroundModel backgroundModel = new();
        PlayerEntityPresenter playerPresenter;

        IDisposable sceneNavigationSubscription;

        public IObservable<Unit> OnBattleCompleted => battleCompleted;

        bool hasShownTutorial;
        // StartStage 直後の最初の Section で背景の初期状態を決める
        bool awaitingFirstSection;

        // コンティニュー回数。バトルシーン内でのみ保持し、シーン遷移でリセットされる。
        int continueCount;
        IDisposable tutorialClosedSubscription;

        // 再生中の会話。会話シーンの読み込み・コマンドの受け取り・終了の購読をまとめて破棄できるようにする
        ScenarioCommandQueue currentScenario;
        readonly CompositeDisposable scenarioSubscriptions = new();
        bool isSceneLoadedHandlerRegistered;
        bool restorePlayerControlAfterScenario;

        void Awake()
        {
            battleClock = GetComponent<BattleClock>() ?? gameObject.AddComponent<BattleClock>();
            flow = new BattleFlow(battleClock, enemyTracker, bulletClearReceiver,
                () => soundManager != null ? soundManager.BgmAudioSource : null,
                se => soundManager?.PlaySE(se));
            flow.Background = backgroundPresenter;
            flow.Scenario = this;
            flow.PlayBgm = PlayBgm;
            flow.SectionStarted += OnSectionStarted;
            flow.BossSpawned += OnBossSpawned;
            flow.BossBattleStarted += OnBossBattleStarted;
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

            // 1面の頭から始めるときはチュートリアルモーダルを表示し、閉じてから開始する
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
            if (!isStage1 || runtimeModel.CheckpointSection != 0) return false;

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
            StopBattle();

            if (bulletClearReceiver != null)
            {
                bulletClearReceiver.ClearAllBullets();
                bulletClearReceiver.ClearAllEnemies();
            }

            playerPresenter?.Retry();
            // コンティニュー時のみ回数を進め、テーブルに応じた最大HPを適用する（Retryによる全回復の後に上書き）
            if (isContinue) continueCount++;
            ApplyPlayerMaxHp();
            playerPresenter?.SetColliderActive(true);
            playerPresenter?.SubscribeToAttackInput();
            playerPresenter?.UnfreezeRunAnimation();

            StartStage(RuntimeModelRepository.Get().CheckpointSection, resume: true);
        }

        async void ReturnToTitle()
        {
            StopBattle();

            var battleSceneName = SceneManager.GetSceneByName(SceneRouterModel.Battle).name;
            await SceneManager.LoadSceneAsync(SceneRouterModel.Title, LoadSceneMode.Additive).ToUniTask();
            SceneManager.SetActiveScene(SceneManager.GetSceneByName(SceneRouterModel.Title));
            SceneManager.UnloadSceneAsync(battleSceneName).ToUniTask().Forget();
        }

        // ツリー・ボス・会話を止める。会話は Scenario ノードの Abort 経由でも閉じるが、念のためここでも後始末する
        void StopBattle()
        {
            flow.Stop();
            playerPresenter?.CancelCharge();
            CloseScenario();
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
            flow.Player = playerPresenter;
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

            stage = LoadStage(stageModel.StageAddress);
            if (stage == null)
            {
                Debug.LogError("No battle stage is configured for this stage.", this);
                return;
            }

            // 途中の Section から始める (デバッグ起動など) ときは、その Section の会話を飛ばす
            var startSection = RuntimeModelRepository.Get().CheckpointSection;
            StartStage(startSection, resume: startSection > 0);
        }

        void StartStage(int section, bool resume)
        {
            awaitingFirstSection = true;
            flow.StartStage(stage, section, resume, OnStageFinished);
        }

        void OnSectionStarted(int index, BattleSituation situation, bool resumed)
        {
            RuntimeModelRepository.Get().CheckpointSection = index;

            if (!awaitingFirstSection) return;
            awaitingFirstSection = false;
            backgroundPresenter?.ResetScroll(situation == BattleSituation.Way);
        }

        void OnBossSpawned(BossEntityPresenter boss)
        {
            playerPresenter?.FreezeRunAnimation();
            backgroundPresenter?.StartDeceleration();
        }

        void OnBossBattleStarted(BossEntityPresenter boss)
        {
            // シナリオ明けにプレイヤーとボスが重なっていても即被弾しないよう、少しの間ボスの当たり判定を切る
            boss.DisableColliderOnEntry();
            playerPresenter?.UnfreezeRunAnimation();
        }

        void OnStageFinished(NodeStatus status)
        {
            if (status == NodeStatus.Success)
            {
                CompleteStage();
                return;
            }
            Debug.LogError($"[BattleScenePresenter] ステージのツリーが {status} で終了しました", this);
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

        void PlayBgm(BgmType bgmType)
        {
            if (soundManager == null || stageModel == null) return;

            // SceneType.Stage1=3, Stage2=4, ... なので stageNumber+2 で変換
            var sceneType = (SceneType)(stageModel.StageNumber + 2);
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

        // ---------- IScenarioService ----------

        public IScenarioHandle Play(ScenarioData data, bool skip, bool restorePlayerControl)
        {
            if (skip || data == null) return ScenarioCommandQueue.Skipped(data);

            CloseScenario();
            currentScenario = new ScenarioCommandQueue(CloseScenario);
            restorePlayerControlAfterScenario = restorePlayerControl;

            // シナリオ中は攻撃を禁止 + プレイヤーの当たり判定を無効化（被弾でゲームオーバー誤発火を防ぐ）
            playerPresenter?.UnsubscribeFromAttackInput();
            playerPresenter?.SetColliderActive(false);
            // チャージ中のままシナリオへ突入した場合に備えてチャージ状態をリセット
            playerPresenter?.CancelCharge();

            var queue = currentScenario;
            MessageBroker.Default.Receive<ScenarioCommandMessage>()
                         .Subscribe(m => queue.Enqueue(new ScenarioStep { function = m.Function, args = m.Args }))
                         .AddTo(scenarioSubscriptions);

            ScenarioModelRepository.Instance.SetData(data);
            if (!isSceneLoadedHandlerRegistered)
            {
                SceneManager.sceneLoaded += OnScenarioSceneLoaded;
                isSceneLoadedHandlerRegistered = true;
            }
            SceneManager.LoadScene(SceneRouterModel.Scenario, LoadSceneMode.Additive);
            return queue;
        }

        void OnScenarioSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != SceneRouterModel.Scenario) return;
            UnregisterSceneLoaded();

            var scenarioPresenter = FindFirstObjectByType<ScenarioScenePresenter>();
            if (scenarioPresenter == null)
            {
                Debug.LogWarning("ScenarioScenePresenter not found in the loaded scene.", this);
                currentScenario?.Complete();
                return;
            }

            var queue = currentScenario;
            scenarioPresenter.OnScenarioCompleted
                             .Take(1)
                             .Subscribe(_ => OnScenarioCompleted(queue))
                             .AddTo(scenarioSubscriptions);
        }

        void OnScenarioCompleted(ScenarioCommandQueue queue)
        {
            if (queue != currentScenario) return;

            playerPresenter?.SetColliderActive(true);
            if (restorePlayerControlAfterScenario) playerPresenter?.SubscribeToAttackInput();

            scenarioSubscriptions.Clear();
            currentScenario = null;
            queue.Complete();
        }

        // 会話を途中で閉じる (リトライ・タイトルへ戻る・ツリーの中断)
        void CloseScenario()
        {
            UnregisterSceneLoaded();
            scenarioSubscriptions.Clear();

            var queue = currentScenario;
            currentScenario = null;
            queue?.Complete();

            var scenarioScene = SceneManager.GetSceneByName(SceneRouterModel.Scenario);
            if (scenarioScene.IsValid() && scenarioScene.isLoaded)
            {
                SceneManager.UnloadSceneAsync(scenarioScene);
            }
            ScenarioModelRepository.Instance.Refresh();
        }

        void UnregisterSceneLoaded()
        {
            if (!isSceneLoadedHandlerRegistered) return;
            SceneManager.sceneLoaded -= OnScenarioSceneLoaded;
            isSceneLoadedHandlerRegistered = false;
        }

        // ---------- クリア ----------

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
            UnregisterSceneLoaded();
            scenarioSubscriptions.Dispose();

            // チュートリアルモーダルを閉じる前にシーンが破棄された場合に備えて後始末する
            tutorialClosedSubscription?.Dispose();
            tutorialClosedSubscription = null;
            var tutorialModal = globalScenePresenter?.TutorialModalPresenter;
            if (tutorialModal != null && tutorialModal.IsOpen)
            {
                tutorialModal.Close();
            }

            disposables.Dispose();
            battleCompleted.Dispose();
            backgroundModel.Dispose();
            flow?.Stop();
        }
    }
}
