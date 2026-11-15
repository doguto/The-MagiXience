using Project.Scenes.Scenario.Scripts.Model;
using Project.Scripts.Extensions;
using Project.Scripts.Model;
using Project.Scripts.Repository.ModelRepository;

namespace Project.Scenes.Scenario.Scripts.Repository.ModelRepository
{
    public class ScenarioModelRepository : ModelRepositoryBase
    {
        public static ScenarioModelRepository Instance { get; } = new();

        ScenarioModel scenarioModel;
        readonly RuntimeModel runtimeModel;

        // 次に会話シーンを開いたときに流すシナリオ。バトルのツリー (Scenario ノード) や撮影用シーンが指定する
        ScenarioData data;

        public ScenarioModelRepository()
        {
            runtimeModel = RuntimeModelRepository.Instance.Get();
        }

        public ScenarioModel Get()
        {
            if (scenarioModel == null)
            {
                if (data == null) throw new System.InvalidOperationException("[ScenarioModelRepository] ScenarioData is not set. Call SetData before opening the scenario scene.");

                scenarioModel = new ScenarioModel();
                scenarioModel.LoadData(data.steps);

                var stageData = StageModelRepository.Instance.GetByStageNumber(runtimeModel.CurrentStageType.AsInt()).StageData;
                scenarioModel.LoadCharacterSprites(stageData.charaStillAddress);
                scenarioModel.SetEnemyStillLayout(stageData.scenarioEnemyStillOffset, stageData.scenarioEnemyStillScale);
            }

            return scenarioModel;
        }

        public void Refresh()
        {
            scenarioModel = null;
            data = null;
        }

        public void SetData(ScenarioData scenario)
        {
            data = scenario;
            scenarioModel = null;
        }
    }
}
