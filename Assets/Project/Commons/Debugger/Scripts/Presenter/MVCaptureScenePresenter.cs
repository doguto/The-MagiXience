#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Linq;
using Cysharp.Threading.Tasks;
using Project.Scenes.Battle.Scripts.Model;
using Project.Scenes.Battle.Scripts.Presenter;
using Project.Scenes.Scenario.Scripts.Model;
using Project.Scenes.Scenario.Scripts.Repository.ModelRepository;
using Project.Scripts.Extensions;
using Project.Scripts.Model;
using Project.Scripts.Presenter;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Project.Commons.Debugger.Scripts.Presenter
{
    /// <summary>
    /// 告知MVなどの撮影用シーンの進行役。
    /// 指定したステージの背景の上でシナリオを流し、Eキーで DemoClear を重ねて表示する。
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
        bool isDemoClearRequested;

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
            SceneManager.LoadSceneAsync(SceneRouterModel.Scenario, LoadSceneMode.Additive);
        }

        void Update()
        {
            if (isDemoClearRequested) return;

            var keyboard = Keyboard.current;
            if (keyboard == null || !keyboard.eKey.wasPressedThisFrame) return;

            isDemoClearRequested = true;
            LoadDemoClear().Forget();
        }

        async UniTaskVoid LoadDemoClear()
        {
            await SceneManager.LoadSceneAsync(SceneRouterModel.DemoClear, LoadSceneMode.Additive)
                              .ToUniTask(cancellationToken: destroyCancellationToken);

            // DemoClearのCanvasは本編ではシナリオ終了後に出るため、シナリオのCanvasより下に描画される設定になっている。
            // 撮影ではシナリオに重ねたいので、シナリオのCanvasより手前に出す
            var scenarioCanvas = FindRootCanvas(SceneRouterModel.Scenario);
            var demoClearCanvas = FindRootCanvas(SceneRouterModel.DemoClear);
            if (scenarioCanvas == null || demoClearCanvas == null) return;

            demoClearCanvas.sortingLayerID = scenarioCanvas.sortingLayerID;
            demoClearCanvas.sortingOrder = scenarioCanvas.sortingOrder + 1;
        }

        static Canvas FindRootCanvas(string sceneName)
        {
            var scene = SceneManager.GetSceneByName(sceneName);
            if (!scene.IsValid() || !scene.isLoaded) return null;

            return scene.GetRootGameObjects()
                        .SelectMany(root => root.GetComponentsInChildren<Canvas>(true))
                        .FirstOrDefault(canvas => canvas.isRootCanvas);
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
