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
        RuntimeModel runtimeModel;

        public ScenarioModelRepository()
        {
            runtimeModel = RuntimeModelRepository.Instance.Get();
        }

        public ScenarioModel Get()
        {
            if (scenarioModel == null)
            {
                scenarioModel = new ScenarioModel();
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
            var scenarioNumber = runtimeModel.GetScenarioNumber();
            
            var path = $"{GamePath.DataStorepath}/scenario_{scenarioNumber}.asset";
            
            // TODO: 本番用シナリオファイルが揃ったらフォールバックを削除
            try
            {
                return UnityEngine.AddressableAssets.Addressables.LoadAssetAsync<ScenarioData>(path).WaitForCompletion();
            }
            catch
            {
                // フォールバック: test_scenarioを使用
                UnityEngine.Debug.LogWarning($"[ScenarioModelRepository] scenario_{scenarioNumber} not found, using test_scenario instead.");
                var fallbackPath = $"{GamePath.DataStorepath}/test_scenario.asset";
                return UnityEngine.AddressableAssets.Addressables.LoadAssetAsync<ScenarioData>(fallbackPath).WaitForCompletion();
            }
        }
    }
}

