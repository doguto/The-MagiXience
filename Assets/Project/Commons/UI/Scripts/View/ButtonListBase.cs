using System;
using System.Collections.Generic;
using UniRx;
using UnityEngine;

namespace Project.Commons.UI.Scripts.View
{
    public abstract class ButtonListBase : MonoBehaviour
    {
        [SerializeField] protected List<ArchivedButtonBase> buttons;

        protected ButtonListType buttonListType;
        
        readonly Subject<int> onButtonChanged = new();
        public IObservable<int> OnButtonChanged => onButtonChanged;

        public int ButtonIndex { get; protected set; }
        public bool IsActive { get; protected set; }
        
        protected virtual bool MoveNextFlag => buttonListType switch
        {
            ButtonListType.Vertical => Input.GetKeyDown(KeyCode.UpArrow),
            ButtonListType.Horizontal => Input.GetKeyDown(KeyCode.RightArrow),
            _ => false
        };
        
        protected virtual bool MoveBackFlag => buttonListType switch
        {
            ButtonListType.Vertical => Input.GetKeyDown(KeyCode.DownArrow),
            ButtonListType.Horizontal => Input.GetKeyDown(KeyCode.LeftArrow),
            _ => false
        };
        
        protected virtual void Update()
        {
            if (!IsActive) return;

            if (MoveNextFlag) MoveNext();
            if (MoveBackFlag) MoveNext(false);
            if (Input.GetKeyDown(KeyCode.Space)) PressButton();
        }
        

        public virtual void Init(ButtonListType buttonListType, int index = 0, bool isActive = false)
        {
            this.buttonListType = buttonListType;
            
            SetButtonIndex(index);
            buttons[ButtonIndex].SetActive(true);
            
            SetActive(isActive);
        }
        
        public void SetActive(bool active)
        {
            IsActive = active;
        }

        public void SetActiveButton(int index, bool isActive = false)
        {
            SetButtonIndex(index);
            buttons[ButtonIndex].SetActive(isActive);
        }
        
        public IObservable<Unit> GetButtonEvent(int index)
        {
            return buttons[index].OnPressed;
        }
        
        public void PressButton() => PressButton(ButtonIndex);
        public void PressButton(int index)
        {
            if (!IsActive) return;
            
            buttons[index].Press();
        }
        
        public abstract void MoveNext(bool isUp = true);
        
        protected void SetButtonIndex(int index)
        {
            onButtonChanged.OnNext(index);
            ButtonIndex = (index + buttons.Count) % buttons.Count;
        }
    }
}