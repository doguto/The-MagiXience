using Project.Commons.UI.Scripts.Presenter;
using Project.Scripts.Extensions;
using Project.Scripts.Model;
using Project.Scripts.Repository.ModelRepository;
using UniRx;
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
        InputSystemUIInputModule uiInputModule;
        InputActionReference pointAction;
        InputActionReference leftClickAction;
        InputActionReference rightClickAction;
        InputActionReference middleClickAction;
        InputActionReference scrollWheelAction;

        public OptionModalPresenter OptionModalPresenter => optionModalPresenter;
        public PauseModalPresenter PauseModalPresenter => pauseModalPresenter;
        public GameOverModalPresenter GameOverModalPresenter => gameOverModalPresenter;
        public TutorialModalPresenter TutorialModalPresenter => tutorialModalPresenter;
        public KeyConfigModel KeyConfigModel => KeyConfigModelRepository.Instance.Get();
        public InputActionAsset InputActions => inputActions;
        public SceneNavigator SceneNavigator { get; } = new SceneNavigator();

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
                    uiInputModule = inputModule;
                    pointAction = inputModule.point;
                    leftClickAction = inputModule.leftClick;
                    rightClickAction = inputModule.rightClick;
                    middleClickAction = inputModule.middleClick;
                    scrollWheelAction = inputModule.scrollWheel;
                }
            }

            inputManager = new InputManager();
            inputManager.Setup(keyConfigModel.GetInputActions());

            // UIのマウス操作はデフォルトで無効にし、音量設定(オプション)画面を開いている間だけ有効にする。
            SetUIMouseEnabled(false);
            optionModalPresenter.OnOpened.Subscribe(_ => SetUIMouseEnabled(true)).AddTo(this);
            optionModalPresenter.OnClosed.Subscribe(_ => SetUIMouseEnabled(false)).AddTo(this);
        }

        // InputModuleからポインタ系のActionを外すことで、UIがマウスのホバー・クリックを拾わないようにする。
        void SetUIMouseEnabled(bool enabled)
        {
            if (uiInputModule == null) return;

            uiInputModule.point = enabled ? pointAction : null;
            uiInputModule.leftClick = enabled ? leftClickAction : null;
            uiInputModule.rightClick = enabled ? rightClickAction : null;
            uiInputModule.middleClick = enabled ? middleClickAction : null;
            uiInputModule.scrollWheel = enabled ? scrollWheelAction : null;
        }
    }
}
