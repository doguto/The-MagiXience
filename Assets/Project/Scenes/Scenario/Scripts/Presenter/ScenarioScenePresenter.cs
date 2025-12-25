using System.Linq;
using Project.Scenes.Scenario.Scripts.Model;
using Project.Scenes.Scenario.Scripts.Repository.ModelRepository;
using Project.Scenes.Scenario.Scripts.View;
using UnityEngine;

namespace Project.Scenes.Scenario.Scripts.Presenter
{
    public class ScenarioScenePresenter : MonoBehaviour
    {
        [SerializeField] ScenarioView scenarioView;

        ScenarioModel scenarioModel;

        void Start()
        {
            // RepositoryからModelを取得
            scenarioModel = ScenarioModelRepository.Instance.Get();

            // 初回実行
            ExecuteSteps();
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Z))
            {
                OnPressNext();
            }
        }

        void OnPressNext()
        {
            if (scenarioModel.IsEnd)
            {
                Debug.Log("Scenario End");
                return;
            }

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
                    
                    scenarioView.ShowCast(characterName, unknownArg1, faceExpression, 
                        displayTime, position, unknownArg2,
                        scenarioModel.PlayerStillSprite, scenarioModel.EnemyStillSprite);
                    
                    scenarioModel.Next();
                    continue;
                }


                scenarioModel.Next();
            }
        }
    }
}
