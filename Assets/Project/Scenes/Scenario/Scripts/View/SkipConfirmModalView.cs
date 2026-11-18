using System;
using Project.Commons.UI.Scripts.View;
using UniRx;
using UnityEngine;

namespace Project.Scenes.Scenario.Scripts.View
{
    public class SkipConfirmModalView : MonoBehaviour
    {
        [SerializeField] SimpleButton enableMashSkipButton;
        [SerializeField] SimpleButton skipOnceButton;
        [SerializeField] SimpleButton cancelButton;

        public IObservable<Unit> OnPressedEnableMashSkip => enableMashSkipButton.OnPressed;
        public IObservable<Unit> OnPressedSkipOnce => skipOnceButton.OnPressed;
        public IObservable<Unit> OnPressedCancel => cancelButton.OnPressed;

        public void InitStart()
        {
            enableMashSkipButton.Init();
            skipOnceButton.Init();
            cancelButton.Init(isFocused: true);
        }
    }
}
