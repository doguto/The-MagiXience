using Project.Commons.UI.Scripts.Presenter;
using Project.Scenes.Global.Scripts.View;
using Project.Scripts.Model;
using Project.Scripts.Repository.ModelRepository;
using UnityEngine;
using UnityEngine.InputSystem;

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
            // InputActionAssetをRepositoryに設定
            if (inputActions != null)
            {
                KeyConfigModelRepository.Instance.Initialize(inputActions);
            }
        }
    }
}
