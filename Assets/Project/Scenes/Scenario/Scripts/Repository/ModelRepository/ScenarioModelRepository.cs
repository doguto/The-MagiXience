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

        // 撮影用シーンなど、ステージ進行と無関係なシナリオを流すときの差し替えデータ。
        // Refreshでクリアされるため、通常のステージ進行には持ち越されない
        ScenarioData overrideData;

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
                var stageData = StageModelRepository.Instance.GetByStageNumber(runtimeModel.CurrentStageType.AsInt()).StageData;
                scenarioModel.LoadCharacterSprites(stageData.charaStillAddress);
                scenarioModel.SetEnemyStillLayout(stageData.scenarioEnemyStillOffset, stageData.scenarioEnemyStillScale);
            }

            return scenarioModel;
        }

        public void Refresh()
        {
            scenarioModel = null;
            overrideData = null;
        }

        /// <summary>
        /// 次にGetで生成するシナリオを、ステージ番号・Situationから決まるものではなく指定データにする。
        /// </summary>
        public void SetOverrideData(ScenarioData data)
        {
            overrideData = data;
            scenarioModel = null;
        }

        ScenarioData LoadData()
        {
            if (overrideData != null)
            {
                Debug.Log($"[ScenarioModelRepository] Using override scenario: {overrideData.name}");
                return overrideData;
            }

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
    }
}
