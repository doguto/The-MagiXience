using System.Linq;
using Project.Scenes.Scenario.Scripts.Model;
using Project.Scenes.Scenario.Scripts.Repository.ModelRepository;
using Project.Scenes.Scenario.Scripts.View;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Project.Scenes.Scenario.Scripts.Presenter
{
    public class ScenarioScenePresenter : MonoBehaviour
    {
        [SerializeField] ScenarioView scenarioView;
        [SerializeField] Button nextButton;

        ScenarioModel scenarioModel;

        void Start()
        {
            // RepositoryからModelを取得
            scenarioModel = ScenarioModelRepository.Instance.Get();

            nextButton.OnClickAsObservable().Subscribe(_ => OnClickNext()).AddTo(this);

            // 初回実行
            ExecuteSteps();
        }

        void OnClickNext()
        {
            if (scenarioModel.IsEnd)
            {
                Debug.Log("Scenario End");
                return;
            }

            // 次へ進む (現在のメッセージを読み終わった扱い)
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
                if (step.function == "ShowCastMessage" || step.function == "ShowMessage")
                {
                    // View等への反映はここで行う (今回はログのみ)
                    break;
                }

                // それ以外は自動で次へ
                scenarioModel.Next();
            }
        }
    }
}
