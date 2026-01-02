using System;
using System.Collections.Generic;
using UniRx;
using UnityEngine;

namespace Project.Commons.UI.Scripts.View
{
    public class OptionModalView
    {
        [SerializeField] SimpleButton canselButton;
        [SerializeField] SimpleButton saveButton;
        [SerializeField] List<SimpleButton> keyConfigButtons;
        
        public IObservable<Unit> OnPressedCansel => canselButton.OnPressed;
        public IObservable<Unit> OnPressedSave => saveButton.OnPressed;

        public IObservable<Unit> OnPressedKeyConfig(int i)
        {
            return keyConfigButtons[i].OnPressed;
        }

        public void InitStart()
        {
            canselButton.Init(isFocused:true);
            saveButton.Init();
            foreach (var keyConfigButton in keyConfigButtons)
            {
                keyConfigButton.Init();
            }
        }
        
    }
}
