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
        const float TargetAspect = 16f / 9f;
        int lastScreenWidth;
        int lastScreenHeight;

        void Update()
        {
            KeepTargetAspect();
        }

        // ウィンドウのリサイズ・フルスクリーン切り替えで16:9から外れたら解像度を補正する。
        // 非16:9ディスプレイでのフルスクリーン(FullScreenWindow)は、Unityが黒帯を付けて表示する。
        void KeepTargetAspect()
        {
            if (Application.isEditor) return;

            var width = Screen.width;
            var height = Screen.height;
            if (width <= 0 || height <= 0) return; // 最小化中
            if (width == lastScreenWidth && height == lastScreenHeight) return;

            var widthChanged = Mathf.Abs(width - lastScreenWidth) >= Mathf.Abs(height - lastScreenHeight);
            lastScreenWidth = width;
            lastScreenHeight = height;
            if (Mathf.Abs((float)width / height - TargetAspect) < 0.01f) return;

            // ウィンドウ時は主に変化した辺を基準にし、フルスクリーン時は画面に収まる最大サイズにする。
            var mode = Screen.fullScreenMode;
            var displayInfo = Screen.mainWindowDisplayInfo;
            var maxWidth = mode == FullScreenMode.Windowed ? displayInfo.workArea.width : displayInfo.width;
            var maxHeight = mode == FullScreenMode.Windowed ? displayInfo.workArea.height : displayInfo.height;
            if (mode != FullScreenMode.Windowed) widthChanged = (float)width / height < TargetAspect;

            var newWidth = widthChanged ? width : Mathf.RoundToInt(height * TargetAspect);
            if (newWidth > maxWidth) newWidth = maxWidth;
            var newHeight = Mathf.RoundToInt(newWidth / TargetAspect);
            if (newHeight > maxHeight)
            {
                newHeight = maxHeight;
                newWidth = Mathf.RoundToInt(newHeight * TargetAspect);
            }

            Screen.SetResolution(newWidth, newHeight, mode);
        }

        void Awake()
        {
            if (inputActions == null)
            {
                Debug.LogError("InputActionsが設定されていません", this);
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
