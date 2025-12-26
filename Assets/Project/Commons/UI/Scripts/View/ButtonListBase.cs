using System;
using System.Collections.Generic;
using UniRx;
using UnityEngine;

namespace Project.Commons.UI.Scripts.View
{
    public abstract class ButtonListBase : MonoBehaviour
    {
        [SerializeField] protected List<MovableButton> buttons;

        protected ButtonListType buttonListType;

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
            if (MoveBackFlag) MoveBack();
        }
        
        void MoveBack() => MoveNext(false);
        

        public virtual void Init(ButtonListType buttonListType, int index = 0, bool isActive = false)
        {
            this.buttonListType = buttonListType;
            
            SetButtonIndex(index);
            SetActive(isActive);
        }
        
        public void SetActive(bool active)
        {
            IsActive = active;
        }

        public abstract void MoveNext(bool isUp = true);
        
        protected void SetButtonIndex(int index)
        {
            ButtonIndex = (index + buttons.Count) % buttons.Count;
        }
    }
}
