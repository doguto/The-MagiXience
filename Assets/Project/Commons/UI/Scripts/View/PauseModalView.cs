using System;
using UniRx;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Project.Commons.UI.Scripts.View
{
    public class PauseModalView: MonoBehaviour
    {
        [SerializeField] SimpleButton cancelButton;
        [SerializeField] SimpleButton retryButton;
        [SerializeField] SimpleButton optionButton;
        [SerializeField] SimpleButton exitButton;

        public IObservable<Unit> OnPressedCancel => cancelButton.OnPressed;
        public IObservable<Unit> OnPressedRetry => retryButton.OnPressed;
        public IObservable<Unit> OnPressedOption => optionButton.OnPressed;
        public IObservable<Unit> OnPressedExit => exitButton.OnPressed;

        public void InitStart()
        {
            cancelButton.Init(isFocused:true);
            retryButton.Init();
            optionButton.Init();
            exitButton.Init();

            EventSystem.current.SetSelectedGameObject(cancelButton.gameObject);
        }

    }
}
