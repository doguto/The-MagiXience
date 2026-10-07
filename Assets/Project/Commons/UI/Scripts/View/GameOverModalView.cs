using System;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;

namespace Project.Commons.UI.Scripts.View
{
    public class GameOverModalView : MonoBehaviour
    {
        [SerializeField] SimpleButton retryButton;
        [SerializeField] SimpleButton optionButton;
        [SerializeField] SimpleButton exitButton;
        [FormerlySerializedAs("retryTipText")]
        [SerializeField] TMP_Text tipText;

        const string RetryTipFormat = "Rewrite: 次回リトライ時、主人公の体力を {0}% 増加した状態に書き換える。";
        const string ExitTip = "Return: 物語の進行を一時中断する。";

        public IObservable<Unit> OnPressedRetry => retryButton.OnPressed;
        public IObservable<Unit> OnPressedOption => optionButton.OnPressed;
        public IObservable<Unit> OnPressedTitle => exitButton.OnPressed;

        string retryTip = "";

        void Awake()
        {
            // フォーカス中のボタンに応じてtipsを切り替える
            retryButton.OnFocusedEvent.Subscribe(_ => SetTip(retryTip)).AddTo(this);
            optionButton.OnFocusedEvent.Subscribe(_ => SetTip("")).AddTo(this);
            exitButton.OnFocusedEvent.Subscribe(_ => SetTip(ExitTip)).AddTo(this);
        }

        public void InitStart()
        {
            retryButton.Init(isFocused: true);
            optionButton.Init();
            exitButton.Init();

            EventSystem.current.SetSelectedGameObject(retryButton.gameObject);

            // 既にフォーカス済みだとOnFocusedEventが発火しないため、初期表示は明示的に行う
            SetTip(retryTip);
        }

        // 次回リトライ時のHP増加率(初期HP比, %)をリトライ用のtipsに設定する
        public void SetRetryHpIncreaseRate(int increaseRatePercent)
        {
            retryTip = string.Format(RetryTipFormat, increaseRatePercent);
        }

        void SetTip(string text)
        {
            if (tipText == null) return;
            tipText.text = text;
        }
    }
}
