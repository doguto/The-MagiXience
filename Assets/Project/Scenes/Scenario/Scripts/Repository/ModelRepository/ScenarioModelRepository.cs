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

        public ScenarioModelRepository()
        {
        }

        public ScenarioModel Get()
        {
            if (scenarioModel == null)
            {
                // Init model
                // TODO: 実際のクリアステージ数を取得する
                scenarioModel = new ScenarioModel(1);
                var data = LoadData();
                scenarioModel.LoadData(data.steps);
            }
            return scenarioModel;
        }

        public void Refresh()
        {
            scenarioModel = null;
        }

        ScenarioData LoadData()
        {
            var path = $"{GamePath.DataStorepath}/test_scenario.asset";
            return UnityEngine.AddressableAssets.Addressables.LoadAssetAsync<ScenarioData>(path).WaitForCompletion();
        }
    }
}
