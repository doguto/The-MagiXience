using System;
using System.Collections.Generic;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Project.Commons.UI.Scripts.View
{
    public class OptionModalView: MonoBehaviour
    {
        [SerializeField] SimpleButton cancelButton;
        [SerializeField] SimpleButton saveButton;
        [SerializeField] List<SimpleButton> keyConfigButtons;

        [Header("Volume")]
        [SerializeField] Slider bgmSlider;
        [SerializeField] Slider seSlider;
        [SerializeField] TMP_Text bgmValueText;
        [SerializeField] TMP_Text seValueText;

        readonly Subject<int> onBgmValueChanged = new();
        readonly Subject<int> onSeValueChanged = new();

        public IObservable<Unit> OnPressedCancel => cancelButton.OnPressed;
        public IObservable<Unit> OnPressedSave => saveButton.OnPressed;

        // スライダー操作・キー操作いずれでも値が変化したら発火する(0-100)。
        public IObservable<int> OnBgmValueChanged => onBgmValueChanged;
        public IObservable<int> OnSeValueChanged => onSeValueChanged;
        
        public IObservable<Unit> OnPressedKeyConfig(int i)
        {
            return keyConfigButtons[i].OnPressed;
        }

        public void InitStart()
        {
            cancelButton.Init(isFocused: true);
            saveButton.Init();
            foreach (var keyConfigButton in keyConfigButtons)
            {
                keyConfigButton.Init();
            }
        }

        void Awake()
        {
            // スライダー(マウス操作・後述のキー操作によるvalue変更)の値変化を購読する。
            bgmSlider.OnValueChangedAsObservable()
                .Subscribe(v =>
                {
                    var value = Mathf.RoundToInt(v);
                    bgmValueText.text = value.ToString();
                    onBgmValueChanged.OnNext(value);
                })
                .AddTo(this);

            seSlider.OnValueChangedAsObservable()
                .Subscribe(v =>
                {
                    var value = Mathf.RoundToInt(v);
                    seValueText.text = value.ToString();
                    onSeValueChanged.OnNext(value);
                })
                .AddTo(this);
        }

        // 通知を発火せずにスライダー・テキストへ値を反映する(初期化・外部反映用)。
        public void SetBgmValueWithoutNotify(int value)
        {
            bgmSlider.SetValueWithoutNotify(value);
            bgmValueText.text = value.ToString();
        }

        public void SetSeValueWithoutNotify(int value)
        {
            seSlider.SetValueWithoutNotify(value);
            seValueText.text = value.ToString();
        }
    }
}
