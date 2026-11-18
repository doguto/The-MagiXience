using Project.Commons.UI.Scripts.Presenter;
using Project.Commons.UI.Scripts.View;
using Project.Scripts.Extensions;
using Project.Scripts.Model;
using Project.Scripts.Repository.ModelRepository;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;

namespace Project.Scenes.Global.Scripts.Presenter
{
    // GlobalScenePresenterはGlobalScenePresenterをFindすると変になるため、MonoPresenterを継承しない
    public class GlobalScenePresenter : MonoBehaviour
    {
        [SerializeField] OptionModalPresenter optionModalPresenter;
        [SerializeField] PauseModalPresenter pauseModalPresenter;
        [SerializeField] GameOverModalPresenter gameOverModalPresenter;
        [SerializeField] TutorialModalPresenter tutorialModalPresenter;
        [SerializeField] SoundManagerPresenter soundManagerPresenter;

        public SoundManagerPresenter SoundManagerPresenter => soundManagerPresenter;
        [SerializeField] InputActionAsset inputActions;
        InputManager inputManager;

        public OptionModalPresenter OptionModalPresenter => optionModalPresenter;
        public PauseModalPresenter PauseModalPresenter => pauseModalPresenter;
        public GameOverModalPresenter GameOverModalPresenter => gameOverModalPresenter;
        public TutorialModalPresenter TutorialModalPresenter => tutorialModalPresenter;
        public KeyConfigModel KeyConfigModel => KeyConfigModelRepository.Instance.Get();
        public InputActionAsset InputActions => inputActions;
        public SceneNavigator SceneNavigator { get; } = new();

        // 画面のアスペクト比は16:9に固定する。
        // ウィンドウサイズは補正せず、カメラのViewport Rectで16:9の描画領域を作り、余白は黒帯にする。
        public const float TargetAspect = 16f / 9f;
        public const float OrthographicSize = 5f;

        // 全シーン共通のカメラ。各シーンはカメラを持たず、CanvasはGlobalCameraCanvasで参照する。
        [SerializeField] Camera mainCamera;

        // 黒帯を塗りつぶす全画面のカメラ。Viewport Rectの補正対象から除外する。
        [SerializeField] Camera letterboxCamera;

        void OnEnable()
        {
            RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
        }

        void OnDisable()
        {
            RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
        }

        void OnBeginCameraRendering(ScriptableRenderContext context, Camera targetCamera)
        {
            ApplyTargetAspect(targetCamera);
        }

        void ApplyTargetAspect(Camera targetCamera)
        {
            if (targetCamera.cameraType != CameraType.Game) return;
            if (targetCamera == letterboxCamera) return;
            if (targetCamera.targetTexture != null) return;

            var width = Screen.width;
            var height = Screen.height;
            if (width <= 0 || height <= 0) return; // 最小化中

            var viewportRect = CalculateViewportRect((float)width / height);
            if (targetCamera.rect != viewportRect) targetCamera.rect = viewportRect;
        }

        static Rect CalculateViewportRect(float screenAspect)
        {
            // 横長の画面は左右に黒帯を付ける.
            if (screenAspect > TargetAspect)
            {
                var widthRatio = TargetAspect / screenAspect;
                return new Rect((1f - widthRatio) / 2f, 0f, widthRatio, 1f);
            }

            // 縦長の画面は上下に黒帯を付ける.
            var heightRatio = screenAspect / TargetAspect;
            return new Rect(0f, (1f - heightRatio) / 2f, 1f, heightRatio);
        }

        void Awake()
        {
            if (inputActions == null)
            {
                Debug.LogError("InputActionsが設定されていません", this);
            }

            if (letterboxCamera == null)
            {
                Debug.LogWarning("LetterboxCameraが設定されていません", this);
            }

            if (mainCamera == null)
            {
                Debug.LogError("MainCameraが設定されていません", this);
            }
            else
            {
                mainCamera.orthographicSize = OrthographicSize;
                GlobalCameraCanvas.SetCamera(mainCamera);
            }

            // InputActionAssetをRepositoryに設定
            KeyConfigModelRepository.Instance.Initialize(inputActions);
            var keyConfigModel = KeyConfigModelRepository.Instance.Get();

            // EventSystemのInputModuleにInputActionAssetを設定
            var eventSystem = FindFirstObjectByType<EventSystem>();
            if (eventSystem != null)
            {
                var inputModule = eventSystem.GetComponent<InputSystemUIInputModule>();
                if (inputModule != null)
                {
                    inputModule.actionsAsset = keyConfigModel.GetInputActions();
                }
            }

            inputManager = new InputManager();
            inputManager.Setup(keyConfigModel.GetInputActions());
        }
    }
}
