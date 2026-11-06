using System;
using Project.Scripts.Extensions.Message;
using UniRx;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Project.Commons.UI.Scripts.View
{
    // VolumeBlockGaugeをフォーカス可能にし、左右キーで値を増減する。上下キーはNavigationに従う
    [RequireComponent(typeof(VolumeBlockGauge))]
    public class VolumeGaugeSelectable : Selectable
    {
        [SerializeField, Min(1)] int step = 1;
        [SerializeField, Min(1)] int ctrlStep = 10;

        VolumeBlockGauge gauge;
        bool isCtrlPressed;
        readonly Subject<int> onValueChanged = new();

        public IObservable<int> OnValueChanged => onValueChanged;
        public int Value => Gauge.Value;

        VolumeBlockGauge Gauge => gauge != null ? gauge : gauge = GetComponent<VolumeBlockGauge>();

        protected override void Awake()
        {
            base.Awake();
            if (!Application.isPlaying) return;

            MessageBroker.Default.Receive<UICtrlMessage>()
                .Subscribe(message => isCtrlPressed = message.isPressed)
                .AddTo(this);
        }

        public void SetValueWithoutNotify(int value)
        {
            Gauge.Value = value;
        }

        public override void OnMove(AxisEventData eventData)
        {
            if (!IsActive() || !IsInteractable())
            {
                base.OnMove(eventData);
                return;
            }

            switch (eventData.moveDir)
            {
                case MoveDirection.Left:
                    AddValue(-CurrentStep);
                    break;
                case MoveDirection.Right:
                    AddValue(CurrentStep);
                    break;
                default:
                    base.OnMove(eventData);
                    break;
            }
        }

        int CurrentStep => isCtrlPressed ? ctrlStep : step;

        void AddValue(int delta)
        {
            var before = Gauge.Value;
            Gauge.Value = before + delta;
            if (Gauge.Value != before) onValueChanged.OnNext(Gauge.Value);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            onValueChanged.Dispose();
        }
    }
}
