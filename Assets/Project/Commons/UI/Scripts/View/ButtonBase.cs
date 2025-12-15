using System;
using UniRx;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Project.Commons.UI.Scripts.View
{
    public class ButtonBase : Selectable, ISubmitHandler, IPointerClickHandler
    {
        protected readonly Subject<Unit> onPressed = new();
        public IObservable<Unit> OnPressed => onPressed;

        bool isFocused;
        protected bool IsFocused
        {
            get => isFocused;
            set
            {
                if (value == isFocused) return;
                isFocused = value;

                if (value) OnFocused();
                else OnUnfocused();
            }
        }

        bool isOpened;
        protected bool IsOpened
        {
            get => isOpened;
            set
            {
                if (value == isOpened) return;
                isOpened = value;

                if (value) OnOpened();
                else OnClosed();
            }
        }


        public void Init(bool isOpened = true, bool isFocused = false)
        {
            IsOpened = isOpened;
            IsFocused = isFocused;
        }

        protected virtual void PressButton()
        {
            if (!IsOpened) return;
            if (!IsFocused) return;

            onPressed.OnNext(Unit.Default);
        }

        public override void OnSelect(BaseEventData eventData)
        {
            base.OnSelect(eventData);
            IsFocused = true;
        }

        public override void OnDeselect(BaseEventData eventData)
        {
            base.OnDeselect(eventData);
            IsFocused = false;
        }

        public virtual void OnSubmit(BaseEventData eventData)
        {
            PressButton();
        }

        public virtual void OnPointerClick(PointerEventData eventData)
        {
            IsFocused = true;
            PressButton();
        }

        public override void OnPointerEnter(PointerEventData eventData)
        {
            base.OnPointerEnter(eventData);
            EventSystem.current.SetSelectedGameObject(gameObject);
            IsFocused = true;
        }

        protected virtual void OnFocused() { }
        protected virtual void OnUnfocused() { }
        
        protected virtual void OnOpened() { }
        protected virtual void OnClosed() { }
    }
}
