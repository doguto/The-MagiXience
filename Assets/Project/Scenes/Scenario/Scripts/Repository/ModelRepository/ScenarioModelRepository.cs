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
                var stageType = runtimeModel.CurrentStageType;
                var enemyCharaName = GetEnemyCharaName(stageType.AsInt());
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
            var stageNumber = runtimeModel.CurrentStageType.AsInt();
            var situation = runtimeModel.CurrentSituation;

            var scenarioId = GenerateScenarioId(stageNumber, situation);
            var situationFolder = situation == BattleSituation.Way ? "Way" : "Boss";
            var path = $"{GamePath.DataStorepath}/Stage{stageNumber}/{situationFolder}/{scenarioId}.asset";
            Debug.Log($"[ScenarioModelRepository] Loading scenario: {scenarioId} from {path}");

            var data = UnityEngine.AddressableAssets.Addressables
                                  .LoadAssetAsync<ScenarioData>(path).WaitForCompletion();
            if (data != null)
            {
                Debug.Log($"[ScenarioModelRepository] Loaded scenario: {scenarioId}");
                return data;
            }

            throw new System.Exception($"Failed to load scenario: {scenarioId}");
        }

        string GenerateScenarioId(int stageNumber, BattleSituation situation)
        {
            var situationSuffix = situation == BattleSituation.Way ? "Way" : "Boss";
            return $"Stage{stageNumber}{situationSuffix}Scenario";
        }

        string GetEnemyCharaName(int stageNumber)
        {
            var stageModel = StageModelRepository.Instance.GetByStageNumber(stageNumber);
            return stageModel.StageData.charaStillAddress;
        }
    }
}
