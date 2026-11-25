using System;
using Project.Commons.UI.Scripts.View;
using UniRx;
using UnityEngine;
using UnityEngine.EventSystems;

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

        // EventSystem の選択が外れている (null か非表示のオブジェクトを指している) とき、既定のボタンを選択し直す。
        // 選び直した場合は true を返す
        public bool RestoreSelectionIfLost()
        {
            var eventSystem = EventSystem.current;
            if (eventSystem == null) return false;

            var selected = eventSystem.currentSelectedGameObject;
            if (selected != null && selected.activeInHierarchy) return false;

            eventSystem.SetSelectedGameObject(cancelButton.gameObject);
            return true;
        }
    }
}
