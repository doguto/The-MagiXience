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
        [SerializeField] KeyConfigModalPresenter keyConfigModalPresenter;
        [SerializeField] SoundManagerPresenter soundManagerPresenter;

        public SoundManagerPresenter SoundManagerPresenter => soundManagerPresenter;
        [SerializeField] InputActionAsset inputActions;
        InputManager inputManager;

        public OptionModalPresenter OptionModalPresenter => optionModalPresenter;
        public KeyConfigModalPresenter KeyConfigModalPresenter => keyConfigModalPresenter;
        public KeyConfigModel KeyConfigModel => KeyConfigModelRepository.Instance.Get();
        public InputActionAsset InputActions => inputActions;

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
