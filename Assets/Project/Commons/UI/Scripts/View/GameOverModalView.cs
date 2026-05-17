using System;
using UniRx;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Project.Commons.UI.Scripts.View
{
    public class GameOverModalView : MonoBehaviour
    {
        [SerializeField] SimpleButton retryButton;
        [SerializeField] SimpleButton optionButton;
        [SerializeField] SimpleButton exitButton;

        public IObservable<Unit> OnPressedRetry => retryButton.OnPressed;
        public IObservable<Unit> OnPressedOption => optionButton.OnPressed;
        public IObservable<Unit> OnPressedTitle => exitButton.OnPressed;

        public void InitStart()
        {
            retryButton.Init(isFocused: true);
            optionButton.Init();
            exitButton.Init();

            EventSystem.current.SetSelectedGameObject(retryButton.gameObject);
        }
    }
}
