#if UNITY_EDITOR || DEVELOPMENT_BUILD
using Cysharp.Threading.Tasks;
using Project.Scenes.Battle.Scripts.Model;
using Project.Scenes.Battle.Scripts.Presenter;
using Project.Scenes.Scenario.Scripts.Model;
using Project.Scenes.Scenario.Scripts.Presenter;
using Project.Scenes.Scenario.Scripts.Repository.ModelRepository;
using Project.Scripts.Extensions;
using Project.Scripts.Model;
using Project.Scripts.Presenter;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Project.Commons.Debugger.Scripts.Presenter
{
    /// <summary>
    /// 告知MVなどの撮影用シーンの進行役。
    /// 指定したステージの背景の上でシナリオを流し、終わったら DemoClear を表示する。
    /// </summary>
    public class MVCaptureScenePresenter : MonoPresenter
    {
        [SerializeField] ScenarioData scenarioData;
        [Tooltip("立ち絵とその位置補正に使うステージ番号(ロコ=1)")]
        [SerializeField] [Min(1)] int charaStageNumber = 1;
        [Tooltip("背景に使うステージ番号")]
        [SerializeField] [Min(1)] int backgroundStageNumber = 3;
        [SerializeField] BackgroundPresenter backgroundPresenter;

        readonly BattleBackgroundModel backgroundModel = new();

        protected override void Start()
        {
            base.Start();

            // シナリオの立ち絵はRuntimeModelの現在ステージから決まるため、撮影用のステージを設定しておく
            var runtimeModel = RuntimeModelRepository.Get();
            runtimeModel.CurrentStageType = BattleStageTypeExtensions.FromInt(charaStageNumber);
            runtimeModel.CurrentSituation = BattleSituation.Way;

            backgroundModel.SetStage(StageModelRepository.GetByStageNumber(backgroundStageNumber).BackgroundAddress);
            backgroundPresenter?.Initialize(backgroundModel);

            if (scenarioData == null)
            {
                Debug.LogWarning("[MVCaptureScenePresenter] ScenarioData が未設定のためシナリオを流しません", this);
                return;
            }

            ScenarioModelRepository.Instance.SetOverrideData(scenarioData);
            PlayScenarioThenDemoClear().Forget();
        }

        // 本編のボス戦後と同じく、シナリオが終わった直後に DemoClear を表示する
        async UniTaskVoid PlayScenarioThenDemoClear()
        {
            var token = destroyCancellationToken;
            await SceneManager.LoadSceneAsync(SceneRouterModel.Scenario, LoadSceneMode.Additive)
                              .ToUniTask(cancellationToken: token);

            var scenarioPresenter = FindFirstObjectByType<ScenarioScenePresenter>();
            if (scenarioPresenter == null)
            {
                Debug.LogWarning("[MVCaptureScenePresenter] ScenarioScenePresenter が見つかりません", this);
                return;
            }

            await scenarioPresenter.OnScenarioCompleted.ToUniTask(useFirstValue: true, cancellationToken: token);
            await SceneManager.LoadSceneAsync(SceneRouterModel.DemoClear, LoadSceneMode.Additive)
                              .ToUniTask(cancellationToken: token);
        }

        void OnDestroy()
        {
            backgroundModel.Dispose();
            ScenarioModelRepository.Instance.Refresh();
            RuntimeModelRepository.Get().ExitStage();
        }
    }
}
#endif
