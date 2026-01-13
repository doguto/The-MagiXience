using UnityEngine;
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

                // シナリオデータをロード
                var data = LoadData();
                scenarioModel.LoadData(data.steps);

                // キャラクター画像をロード
                var stageNumber = runtimeModel.CurrentStageNumber;
                var enemyCharaName = GetEnemyCharaName(stageNumber);
                scenarioModel.LoadCharacterSprites(enemyCharaName);
            }
            return scenarioModel;
        }

        public void Refresh()
        {
            scenarioModel = null;
        }

        ScenarioData LoadData()
        {
            var stageNumber = runtimeModel.CurrentStageNumber;
            var situation = runtimeModel.CurrentSituation;

            string scenarioId = GenerateScenarioId(stageNumber, situation);
            string situationFolder = situation == GameSituation.Way ? "Way" : "Boss";
            var path = $"{GamePath.DataStorepath}/Stage{stageNumber}/{situationFolder}/{scenarioId}.asset";
            Debug.Log($"[ScenarioModelRepository] Loading scenario: {scenarioId} from {path}");
            try
            {
                var data = UnityEngine.AddressableAssets.Addressables
                    .LoadAssetAsync<ScenarioData>(path).WaitForCompletion();
                if (data != null)
                {
                    Debug.Log($"[ScenarioModelRepository] Loaded scenario: {scenarioId}");
                    return data;
                }
            }
            catch
            {
                Debug.LogWarning($"[ScenarioModelRepository] Failed to load {scenarioId}, using test_scenario.");
            }

            // フォールバック: test_scenarioを使用
            var fallbackPath = $"{GamePath.DataStorepath}/test_scenario.asset";
            return UnityEngine.AddressableAssets.Addressables
                .LoadAssetAsync<ScenarioData>(fallbackPath).WaitForCompletion();
        }

        string GenerateScenarioId(int stageNumber, GameSituation situation)
        {
            string situationSuffix = situation == GameSituation.Way ? "Way" : "Boss";
            return $"Stage{stageNumber}{situationSuffix}Scenario";
        }

        string GetEnemyCharaName(int stageNumber)
        {
            var stageModel = StageModelRepository.Instance.GetByStageNumber(stageNumber);
            return stageModel.StageData.charaStillAddress;
        }
    }
}

