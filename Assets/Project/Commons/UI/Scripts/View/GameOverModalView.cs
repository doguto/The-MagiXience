using System;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Project.Commons.UI.Scripts.View
{
    public class GameOverModalView : MonoBehaviour
    {
        [SerializeField] SimpleButton retryButton;
        [SerializeField] SimpleButton optionButton;
        [SerializeField] SimpleButton exitButton;
        [SerializeField] TMP_Text retryTipText;

        const string RetryTipFormat = "Rewrite: 次回リトライ時、主人公の体力を {0}% 増加した状態に書き換える。";

        public IObservable<Unit> OnPressedRetry => retryButton.OnPressed;
        public IObservable<Unit> OnPressedOption => optionButton.OnPressed;
        public IObservable<Unit> OnPressedTitle => exitButton.OnPressed;

        public void InitStart()
        {
            retryButton.Init(isFocused: true);
            optionButton.Init();
            exitButton.Init();

            EventSystem.current.SetSelectedGameObject(retryButton.gameObject);
        }

        // 次回リトライ時のHP増加率(初期HP比, %)をtipsとして表示する
        public void SetRetryHpIncreaseRate(int increaseRatePercent)
        {
            if (retryTipText == null) return;
            retryTipText.text = string.Format(RetryTipFormat, increaseRatePercent);
        }
    }
}
