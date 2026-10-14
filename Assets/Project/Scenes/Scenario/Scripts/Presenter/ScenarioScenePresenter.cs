using System;
using UniRx;
using Project.Scenes.Battle.Scripts.Presenter;
using Project.Scenes.Global.Scripts.Presenter;
using Project.Scenes.Scenario.Scripts.Model;
using Project.Scenes.Scenario.Scripts.Repository.ModelRepository;
using Project.Scenes.Scenario.Scripts.View;
using Project.Scripts.Extensions.Message;
using Project.Scripts.Model;
using Project.Scripts.Repository.ModelRepository;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Project.Scenes.Scenario.Scripts.Presenter
{
    public class ScenarioScenePresenter : MonoBehaviour
    {
        [SerializeField] ScenarioView scenarioView;

        ScenarioModel scenarioModel;
        GlobalScenePresenter globalScenePresenter;
        readonly Subject<Unit> scenarioCompleted = new();
        readonly CompositeDisposable disposables = new();

        // シナリオ完了処理の多重実行を防ぐガード。
        // 完了時の決定入力を連打すると ExecuteSteps の完了ブロックが再入し、
        // AdvanceToNextSequence が複数回走ってステージ状態が暴走するため。
        bool isCompleted;

        // 自動送り設定。有効中はメッセージ表示後に指定秒数経過で自動的に次へ進む
        bool isAutoAdvanceEnabled;
        float autoAdvanceSeconds;
        // trueのときは秒数の代わりにTypeWriterの1文字分の間隔だけ待つ
        bool isAutoAdvanceIntervalAuto;
        readonly SerialDisposable autoAdvanceTimer = new();

        public IObservable<Unit> OnScenarioCompleted => scenarioCompleted;

        void Start()
        {
            scenarioModel = ScenarioModelRepository.Instance.Get();
            globalScenePresenter = FindFirstObjectByType<GlobalScenePresenter>();

            MessageBroker.Default.Receive<UISubmitMessage>()
                         .Where(_ => !IsPaused())
                         .Subscribe(_ => OnPressNext())
                         .AddTo(disposables);

            // 初回実行
            ExecuteSteps();
        }

        bool IsPaused()
        {
            return globalScenePresenter != null
                && globalScenePresenter.PauseModalPresenter != null
                && globalScenePresenter.PauseModalPresenter.IsOpen;
        }

        void OnPressNext()
        {
            if (isCompleted) return;

            // 文字送り中なら全文を表示するだけで、次の会話には進まない
            if (scenarioView.IsTyping)
            {
                scenarioView.CompleteTyping();
                return;
            }

            // 手動で進めた場合は待機中の自動送りを破棄する
            autoAdvanceTimer.Disposable = null;

            scenarioModel.Next();
            ExecuteSteps();
        }

        void ExecuteSteps()
        {
            while (!scenarioModel.IsEnd)
            {
                var step = scenarioModel.CurrentStep;

                // コマンド実行ログ
                scenarioView.LogCommand(step.function, step.args);

                // memo: 将来ルビ表示コマンド作るかも

                // メッセージ表示系コマンドの場合はループを抜けて待機
                if (step.function == "ShowCastMessage")
                {
                    // args[0]: キャラ名 args[1]: 会話内容
                    var characterName = step.args.Length > 0 ? step.args[0] : "";
                    var message = step.args.Length > 1 ? step.args[1] : "";
                    // 自動送りのタイマーは全文表示が終わってから開始する
                    scenarioView.ShowCastMessage(characterName, message, StartAutoAdvanceTimerIfEnabled);
                    break;
                }

                if (step.function == "ShowMessage")
                {
                    var characterName = step.args.Length > 0 ? step.args[0] : "";
                    var message = step.args.Length > 1 ? step.args[1] : "";
                    scenarioView.ShowMessage(characterName, message, StartAutoAdvanceTimerIfEnabled);
                    break;
                }

                if (step.function == "TypeWriter")
                {
                    // args[0]: true/false, args[1]: 1秒あたりの表示文字数
                    if (step.args.Length < 1 || !bool.TryParse(step.args[0], out var isTypeWriterEnabled))
                    {
                        Debug.LogWarning($"[ScenarioScenePresenter] TypeWriter: 有効フラグ '{(step.args.Length > 0 ? step.args[0] : "")}' をパースできません。無効として扱います。");
                        isTypeWriterEnabled = false;
                    }

                    var charactersPerSecond = 0f;
                    if (isTypeWriterEnabled
                        && (step.args.Length < 2 || !float.TryParse(step.args[1], out charactersPerSecond) || charactersPerSecond <= 0f))
                    {
                        Debug.LogWarning($"[ScenarioScenePresenter] TypeWriter: 表示速度 '{(step.args.Length > 1 ? step.args[1] : "")}' をパースできません。");
                        charactersPerSecond = 20f;
                    }

                    scenarioView.SetTypeWriter(isTypeWriterEnabled, charactersPerSecond);

                    scenarioModel.Next();
                    continue;
                }

                if (step.function == "AutoAdvance")
                {
                    // args[0]: true/false, args[1]: 秒数 または auto(TypeWriterの1文字分の間隔)
                    if (step.args.Length < 1 || !bool.TryParse(step.args[0], out isAutoAdvanceEnabled))
                    {
                        Debug.LogWarning($"[ScenarioScenePresenter] AutoAdvance: 有効フラグ '{(step.args.Length > 0 ? step.args[0] : "")}' をパースできません。無効として扱います。");
                        isAutoAdvanceEnabled = false;
                    }

                    isAutoAdvanceIntervalAuto = false;
                    if (isAutoAdvanceEnabled)
                    {
                        if (step.args.Length >= 2 && string.Equals(step.args[1], "auto", StringComparison.OrdinalIgnoreCase))
                        {
                            isAutoAdvanceIntervalAuto = true;
                        }
                        else if (step.args.Length < 2 || !float.TryParse(step.args[1], out autoAdvanceSeconds) || autoAdvanceSeconds < 0f)
                        {
                            Debug.LogWarning($"[ScenarioScenePresenter] AutoAdvance: 秒数 '{(step.args.Length > 1 ? step.args[1] : "")}' をパースできません。");
                            autoAdvanceSeconds = 2f;
                        }
                    }
                    else
                    {
                        autoAdvanceTimer.Disposable = null;
                    }

                    scenarioModel.Next();
                    continue;
                }

                if (step.function == "ShowCast")
                {
                    // args[0]: キャラ名, args[1]: 立ち絵種別(Crazyで発狂立ち絵), args[2]: 表情差分,
                    // args[3]: 表示時間, args[4]: 位置(LL/RR), args[5]: unknown
                    var characterName = step.args.Length > 0 ? step.args[0] : "";
                    var stillType = step.args.Length > 1 ? step.args[1] : "";
                    var faceExpression = step.args.Length > 2 ? step.args[2] : "";
                    var displayTime = step.args.Length > 3 ? step.args[3] : "";
                    var position = step.args.Length > 4 ? step.args[4] : "";
                    var unknownArg2 = step.args.Length > 5 ? step.args[5] : "";

                    // args[1]がCrazyのときは発狂立ち絵、それ以外は通常立ち絵を使用
                    // 発狂立ち絵が用意されていないキャラの場合は通常立ち絵で代用する
                    var isCrazy = stillType == "Crazy";
                    var playerStill = scenarioModel.PlayerStillSprite;
                    var enemyStill = isCrazy && scenarioModel.EnemyCrazyStillSprite != null
                        ? scenarioModel.EnemyCrazyStillSprite
                        : scenarioModel.EnemyStillSprite;

                    // 表情Spriteを取得
                    Sprite faceSprite = null;
                    if (position == "LL" && scenarioModel.PlayerFaceSprites != null)
                    {
                        scenarioModel.PlayerFaceSprites.TryGetValue(faceExpression, out faceSprite);
                    }
                    else if (position == "RR" && scenarioModel.EnemyFaceSprites != null)
                    {
                        scenarioModel.EnemyFaceSprites.TryGetValue(faceExpression, out faceSprite);
                    }

                    scenarioView.ShowCast(characterName, stillType, faceExpression,
                        displayTime, position, unknownArg2,
                        playerStill, enemyStill,
                        faceSprite);

                    scenarioModel.Next();
                    continue;
                }

                if (step.function == "SpawnBoss")
                {
                    var battleScenePresenter = FindFirstObjectByType<BattleScenePresenter>();
                    if (battleScenePresenter != null)
                    {
                        battleScenePresenter.SpawnBoss();
                    }
                    else
                    {
                        Debug.LogWarning("[ScenarioScenePresenter] BattleScenePresenter not found, cannot spawn boss.");
                    }

                    scenarioModel.Next();
                    continue;
                }

                if (step.function == "PlayBossBgm")
                {
                    var battleScenePresenter = FindFirstObjectByType<BattleScenePresenter>();
                    if (battleScenePresenter != null)
                    {
                        battleScenePresenter.PlayBossBgm();
                    }
                    else
                    {
                        Debug.LogWarning("[ScenarioScenePresenter] BattleScenePresenter not found, cannot play boss BGM.");
                    }

                    scenarioModel.Next();
                    continue;
                }

                if (step.function == "HideCast")
                {
                    var characterName = step.args.Length > 0 ? step.args[0] : "";

                    float fadeDuration;
                    if (!float.TryParse(step.args[1], out fadeDuration))
                    {
                        Debug.LogWarning($"[ScenarioScenePresenter] HideCast: fadeDuration '{step.args[1]}' をパースできません。");
                        fadeDuration = 0.5f;
                    }

                    // フェードアウト演出は待たずに進行する
                    scenarioView.HideCast(characterName, fadeDuration);

                    scenarioModel.Next();
                    continue;
                }

                if (step.function == "ChangeCastAnimation")
                {
                    var characterName = step.args.Length > 0 ? step.args[0] : "";
                    var faceExpression = step.args.Length > 1 ? step.args[1] : "";

                    // キャラ名から位置を判定(簡易実装)
                    bool isPlayer = characterName == "テン";

                    // 表情Spriteを取得
                    Sprite faceSprite = null;
                    if (isPlayer && scenarioModel.PlayerFaceSprites != null)
                    {
                        scenarioModel.PlayerFaceSprites.TryGetValue(faceExpression, out faceSprite);
                    }
                    else if (!isPlayer && scenarioModel.EnemyFaceSprites != null)
                    {
                        scenarioModel.EnemyFaceSprites.TryGetValue(faceExpression, out faceSprite);
                    }

                    scenarioView.ChangeFaceExpression(faceSprite);

                    scenarioModel.Next();
                    continue;
                }

                scenarioModel.Next();
            }

            if (scenarioModel.IsEnd)
            {
                if (isCompleted) return;
                isCompleted = true;

                Debug.Log("[ScenarioScenePresenter] Scenario End. Advance to Next Sequence");

                // シナリオ完了時にSituationを変更
                var runtimeModel = RuntimeModelRepository.Instance.Get();
                runtimeModel.AdvanceToNextSequence();

                scenarioCompleted.OnNext(Unit.Default);
                // シナリオシーンをアンロード
                SceneManager.UnloadSceneAsync(SceneRouterModel.Scenario);
                ScenarioModelRepository.Instance.Refresh();
            }
        }

        void StartAutoAdvanceTimerIfEnabled()
        {
            if (!isAutoAdvanceEnabled) return;

            var seconds = autoAdvanceSeconds;
            if (isAutoAdvanceIntervalAuto)
            {
                if (scenarioView.IsTypeWriterEnabled)
                {
                    seconds = scenarioView.TypeWriterCharacterInterval;
                }
                else
                {
                    Debug.LogWarning("[ScenarioScenePresenter] AutoAdvance: auto指定ですがTypeWriterが無効のため2秒で送ります。");
                    seconds = 2f;
                }
            }

            // Scheduler.MainThread はtimeScaleの影響を受けるため、ポーズ中(timeScale=0)はタイマーも止まる
            autoAdvanceTimer.Disposable = Observable.Timer(TimeSpan.FromSeconds(seconds), Scheduler.MainThread)
                                                    .Subscribe(_ => OnPressNext());
        }

        void OnDestroy()
        {
            autoAdvanceTimer.Dispose();
            disposables.Dispose();
            scenarioCompleted.Dispose();
        }
    }
}
