using System;
using System.Collections.Generic;
using TMPro;
using UniRx;
using UniRx.Triggers;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Project.Commons.UI.Scripts.View
{
    public class OptionModalView: MonoBehaviour
    {
        [SerializeField] SimpleButton cancelButton;
        [SerializeField] List<SimpleButton> keyConfigButtons;

        [Header("Volume")]
        [SerializeField] VolumeGaugeSelectable bgmVolume;
        [SerializeField] VolumeGaugeSelectable seVolume;
        [SerializeField] TMP_Text bgmValueText;
        [SerializeField] TMP_Text seValueText;

        [Header("Scenario")]
        [SerializeField] Toggle mashSkipToggle;
        // 有効な側を囲む枠
        [SerializeField] GameObject mashSkipOnFrame;
        [SerializeField] GameObject mashSkipOffFrame;

        readonly Subject<int> onBgmValueChanged = new();
        readonly Subject<int> onSeValueChanged = new();

        public IObservable<Unit> OnPressedCancel => cancelButton.OnPressed;

        // キー操作で値が変化したら発火する(0-100)。
        public IObservable<int> OnBgmValueChanged => onBgmValueChanged;
        public IObservable<int> OnSeValueChanged => onSeValueChanged;
        public IObservable<bool> OnMashSkipChanged => mashSkipToggle.OnValueChangedAsObservable().Skip(1);

        // SE音量がEventSystemで選択された/選択解除されたタイミングで発火する。
        public IObservable<BaseEventData> OnSeVolumeSelected =>
            seVolume.OnSelectAsObservable();
        public IObservable<BaseEventData> OnSeVolumeDeselected =>
            seVolume.OnDeselectAsObservable();

        public IObservable<Unit> OnPressedKeyConfig(int i)
        {
            return keyConfigButtons[i].OnPressed;
        }

        public void InitStart()
        {
            cancelButton.Init(isFocused: true);
            foreach (var keyConfigButton in keyConfigButtons)
            {
                keyConfigButton.Init();
            }
        }

        void Awake()
        {
            bgmVolume.OnValueChanged
                .Subscribe(value =>
                {
                    bgmValueText.text = value.ToString();
                    onBgmValueChanged.OnNext(value);
                })
                .AddTo(this);

            seVolume.OnValueChanged
                .Subscribe(value =>
                {
                    seValueText.text = value.ToString();
                    onSeValueChanged.OnNext(value);
                })
                .AddTo(this);

            // 左右キーでON/OFFを切り替える(左:ON 右:OFF)
            mashSkipToggle.OnMoveAsObservable()
                .Subscribe(e =>
                {
                    if (e.moveDir == MoveDirection.Left) mashSkipToggle.isOn = true;
                    else if (e.moveDir == MoveDirection.Right) mashSkipToggle.isOn = false;
                })
                .AddTo(this);

            mashSkipToggle.OnValueChangedAsObservable()
                .Subscribe(UpdateMashSkipFrame)
                .AddTo(this);
        }

        void UpdateMashSkipFrame(bool isOn)
        {
            if (mashSkipOnFrame != null) mashSkipOnFrame.SetActive(isOn);
            if (mashSkipOffFrame != null) mashSkipOffFrame.SetActive(!isOn);
        }

        // 通知を発火せずにゲージ・テキストへ値を反映する(初期化・外部反映用)。
        public void SetBgmValueWithoutNotify(int value)
        {
            bgmVolume.SetValueWithoutNotify(value);
            bgmValueText.text = bgmVolume.Value.ToString();
        }

        public void SetSeValueWithoutNotify(int value)
        {
            seVolume.SetValueWithoutNotify(value);
            seValueText.text = seVolume.Value.ToString();
        }

        public void SetMashSkipWithoutNotify(bool isEnabled)
        {
            mashSkipToggle.SetIsOnWithoutNotify(isEnabled);
            UpdateMashSkipFrame(isEnabled);
        }
    }
}
