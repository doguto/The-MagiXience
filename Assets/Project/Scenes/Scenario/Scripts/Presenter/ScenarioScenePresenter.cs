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

        public IObservable<Unit> OnScenarioCompleted => scenarioCompleted;

        void Start()
        {
            Debug.Log("[ScenarioScenePresenter] Start called");
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
                    scenarioView.ShowCastMessage(characterName, message);
                    break;
                }

                if (step.function == "ShowMessage")
                {
                    var characterName = step.args.Length > 0 ? step.args[0] : "";
                    var message = step.args.Length > 1 ? step.args[1] : "";
                    scenarioView.ShowMessage(characterName, message);
                    break;
                }

                if (step.function == "ShowCast")
                {
                    // args[0]: キャラ名, args[1]: unknown, args[2]: 表情差分, 
                    // args[3]: 表示時間, args[4]: 位置(LL/RR), args[5]: unknown
                    var characterName = step.args.Length > 0 ? step.args[0] : "";
                    var unknownArg1 = step.args.Length > 1 ? step.args[1] : "";
                    var faceExpression = step.args.Length > 2 ? step.args[2] : "";
                    var displayTime = step.args.Length > 3 ? step.args[3] : "";
                    var position = step.args.Length > 4 ? step.args[4] : "";
                    var unknownArg2 = step.args.Length > 5 ? step.args[5] : "";

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

                    scenarioView.ShowCast(characterName, unknownArg1, faceExpression,
                        displayTime, position, unknownArg2,
                        scenarioModel.PlayerStillSprite, scenarioModel.EnemyStillSprite,
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
                    scenarioView.HideCast(characterName);

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

        void OnDestroy()
        {
            disposables.Dispose();
            scenarioCompleted.Dispose();
        }
    }
}
