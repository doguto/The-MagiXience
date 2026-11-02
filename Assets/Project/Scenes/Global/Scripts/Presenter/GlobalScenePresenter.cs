using Project.Commons.UI.Scripts.Presenter;
using Project.Scripts.Extensions;
using Project.Scripts.Model;
using Project.Scripts.Repository.ModelRepository;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

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
        public SceneNavigator SceneNavigator { get; } = new SceneNavigator();

        // 画面のアスペクト比は16:9に固定する。
        // ウィンドウサイズは補正せず、カメラのViewport Rectで16:9の描画領域を作り、余白は黒帯にする。
        const float TargetAspect = 16f / 9f;

        // 黒帯を塗りつぶす全画面のカメラ。Viewport Rectの補正対象から除外する。
        [SerializeField] Camera letterboxCamera;
        Camera[] cameraBuffer = new Camera[8];

        void Update()
        {
            KeepTargetAspect();
        }

        void KeepTargetAspect()
        {
            var width = Screen.width;
            var height = Screen.height;
            if (width <= 0 || height <= 0) return; // 最小化中

            var viewportRect = CalculateViewportRect((float)width / height);

            // シーン遷移でカメラが入れ替わるため、毎フレーム全カメラを確認する。
            if (cameraBuffer.Length < Camera.allCamerasCount)
            {
                cameraBuffer = new Camera[Camera.allCamerasCount];
            }

            var cameraCount = Camera.GetAllCameras(cameraBuffer);
            for (var i = 0; i < cameraCount; i++)
            {
                var targetCamera = cameraBuffer[i];
                if (targetCamera == letterboxCamera) continue;
                if (targetCamera.rect != viewportRect) targetCamera.rect = viewportRect;
            }
        }

        static Rect CalculateViewportRect(float screenAspect)
        {
            // 横長の画面は左右に、縦長の画面は上下に黒帯を付ける。
            if (screenAspect > TargetAspect)
            {
                var widthRatio = TargetAspect / screenAspect;
                return new Rect((1f - widthRatio) / 2f, 0f, widthRatio, 1f);
            }

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
