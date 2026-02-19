using System;
using System.Collections.Generic;
using Project.Scripts.Extensions.Message;
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

        void Start()
        {
            MessageBroker.Default.Receive<UINavigateMessage>().Subscribe(Move);
        }

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

        public abstract void Move(UINavigateMessage message);

        protected void SetButtonIndex(int index)
        {
            ButtonIndex = (index + buttons.Count) % buttons.Count;
        }
    }
}
