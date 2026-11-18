using System;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Project.Commons.UI.Scripts.View
{
    public class PauseModalView: MonoBehaviour
    {
        [SerializeField] SimpleButton cancelButton;
        [SerializeField] SimpleButton retryButton;
        [SerializeField] SimpleButton optionButton;
        [SerializeField] SimpleButton exitButton;
        [SerializeField] TMP_Text tipText;

        int retryHpIncreaseRate;
        bool isRetryFromStageStart;
        // tipsの文言はGameOverModalと共通
        string RetryTip => isRetryFromStageStart
            ? GameOverModalView.RetryFromStageStartTip
            : string.Format(GameOverModalView.RetryTipFormat, retryHpIncreaseRate);

        public IObservable<Unit> OnPressedCancel => cancelButton.OnPressed;
        public IObservable<Unit> OnPressedRetry => retryButton.OnPressed;
        public IObservable<Unit> OnPressedOption => optionButton.OnPressed;
        public IObservable<Unit> OnPressedExit => exitButton.OnPressed;

        void Awake()
        {
            // フォーカス中のボタンに応じてtipsを切り替える
            cancelButton.OnFocusedEvent.Subscribe(_ => SetTip("")).AddTo(this);
            retryButton.OnFocusedEvent.Subscribe(_ => SetTip(RetryTip)).AddTo(this);
            optionButton.OnFocusedEvent.Subscribe(_ => SetTip("")).AddTo(this);
            exitButton.OnFocusedEvent.Subscribe(_ => SetTip(GameOverModalView.ExitTip)).AddTo(this);
        }

        // 次回リトライ時のHP増加率(初期HP比, %)をリトライ用のtipsに設定する
        public void SetRetryHpIncreaseRate(int increaseRatePercent)
        {
            retryHpIncreaseRate = increaseRatePercent;
        }

        // オプション「リトライ時にステージの最初からやり直す」の状態をリトライ用のtipsに反映する
        public void SetRetryFromStageStart(bool isEnabled)
        {
            isRetryFromStageStart = isEnabled;
        }

        void SetTip(string text)
        {
            if (tipText == null) return;
            tipText.text = text;
        }

        public void InitStart()
        {
            cancelButton.Init(isFocused:true);
            retryButton.Init();
            optionButton.Init();
            exitButton.Init();

            EventSystem.current.SetSelectedGameObject(cancelButton.gameObject);

            // 初期フォーカスはキャンセルなのでtipsは空
            SetTip("");
        }

    }
}
