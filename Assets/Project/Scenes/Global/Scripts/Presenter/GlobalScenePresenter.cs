using System;
using Project.Commons.UI.Scripts.Presenter;
using UnityEngine;

namespace Project.Scenes.Global.Scripts.Presenter
{
    // GlobalScenePresenterはGlobalScenePresenterをFindすると変になるため、MonoPresenterを継承しない
    public class GlobalScenePresenter : MonoBehaviour
    {
        [SerializeField] OptionModalPresenter optionModalPresenter;
        [SerializeField] SoundManagerPresenter soundManagerPresenter;

        public SoundManagerPresenter SoundManagerPresenter => soundManagerPresenter;
        public OptionModalPresenter OptionModalPresenter => optionModalPresenter;
    }
}
