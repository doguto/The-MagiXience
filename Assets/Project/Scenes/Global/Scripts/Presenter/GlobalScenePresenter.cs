using Project.Commons.UI.Scripts.Presenter;
using Project.Commons.UI.Scripts.View;
using Project.Scenes.Global.Scripts.View;
using Project.Scripts.Model;
using Project.Scripts.Repository.ModelRepository;
using UnityEngine;

namespace Project.Scenes.Global.Scripts.Presenter
{
    // GlobalScenePresenterはGlobalScenePresenterをFindすると変になるため、MonoPresenterを継承しない
    public class GlobalScenePresenter : MonoBehaviour
    {
        [SerializeField] SoundManagerView soundManagerView;
        [SerializeField] OptionModalPresenter optionModalPresenter;
        
        public OptionModalPresenter OptionModalPresenter => optionModalPresenter;
        public KeyConfigModel KeyConfigModel => KeyConfigModelRepository.Instance.Get();
    }
}
