using System;
using UniRx;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Project.Commons.UI.Scripts.View
{
    public class PauseModalView
    {
        [SerializeField] SimpleButton canselButton;
        [SerializeField] SimpleButton optionButton;
        [SerializeField] SimpleButton exitButton;
        
        public IObservable<Unit> OnPressedCansel => canselButton.OnPressed;
        public IObservable<Unit> OnPressedOption => optionButton.OnPressed;
        public IObservable<Unit> OnPressedExit => exitButton.OnPressed;

        public void InitStart()
        {
            canselButton.Init(isFocused:true);
            optionButton.Init();
            exitButton.Init();
            
            EventSystem.current.SetSelectedGameObject(canselButton.gameObject);
        }
        
    }
}
