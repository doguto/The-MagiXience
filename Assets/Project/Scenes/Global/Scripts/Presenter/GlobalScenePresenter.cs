using Project.Commons.UI.Scripts.Presenter;
using Project.Scenes.Global.Scripts.View;
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
        [SerializeField] SoundManagerView soundManagerView;
        [SerializeField] OptionModalPresenter optionModalPresenter;
        [SerializeField] InputActionAsset inputActions;

        public OptionModalPresenter OptionModalPresenter => optionModalPresenter;
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
            var eventSystem = FindObjectOfType<EventSystem>();
            if (eventSystem != null)
            {
                var inputModule = eventSystem.GetComponent<InputSystemUIInputModule>();
                if (inputModule != null)
                {
                    inputModule.actionsAsset = keyConfigModel.GetInputActions();
                }
            }
        }
    }
}
