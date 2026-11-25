using System;
using Project.Scenes.Scenario.Scripts.View;
using Project.Scripts.Extensions.Message;
using UniRx;
using UnityEngine;

namespace Project.Scenes.Scenario.Scripts.Presenter
{
    public enum SkipConfirmResult
    {
        EnableMashSkip,
        SkipOnce,
        Cancel,
    }

    public class SkipConfirmModalPresenter : MonoBehaviour
    {
        [SerializeField] SkipConfirmModalView skipConfirmModalView;
        // 連打の勢いでボタンが即決定されないよう、表示直後の入力を無視する秒数
        [SerializeField] float inputGuardSeconds = 0.5f;

        readonly Subject<SkipConfirmResult> onSelected = new();
        public IObservable<SkipConfirmResult> OnSelected => onSelected;
        public bool IsOpen { get; private set; }

        float openedTime;
        // 選択を選び直したフレーム。同じ押下の Submit がそのまま既定のボタンに届いて決定されないようにする
        int selectionRestoredFrame = -1;

        void Awake()
        {
            skipConfirmModalView.OnPressedEnableMashSkip.Subscribe(_ => Select(SkipConfirmResult.EnableMashSkip)).AddTo(this);
            skipConfirmModalView.OnPressedSkipOnce.Subscribe(_ => Select(SkipConfirmResult.SkipOnce)).AddTo(this);
            skipConfirmModalView.OnPressedCancel.Subscribe(_ => Select(SkipConfirmResult.Cancel)).AddTo(this);

            // Esc(UI Cancel) は「しない」を選んだのと同じ扱いで閉じる
            MessageBroker.Default.Receive<UICancelMessage>()
                         .Where(_ => IsOpen)
                         .Subscribe(_ => Select(SkipConfirmResult.Cancel))
                         .AddTo(this);

            // 他のモーダルが選択を奪ったまま閉じた場合などに備え、選択が外れていたら既定のボタンを選び直す
            MessageBroker.Default.Receive<UINavigateMessage>()
                         .Where(_ => IsOpen)
                         .Subscribe(_ => RestoreSelectionIfLost())
                         .AddTo(this);
            MessageBroker.Default.Receive<UISubmitMessage>()
                         .Where(_ => IsOpen)
                         .Subscribe(_ => RestoreSelectionIfLost())
                         .AddTo(this);

            if (!IsOpen)
            {
                gameObject.SetActive(false);
            }
        }

        public void Open()
        {
            if (IsOpen) return;
            IsOpen = true;
            openedTime = Time.unscaledTime;

            gameObject.SetActive(true);
            skipConfirmModalView.InitStart();
        }

        void RestoreSelectionIfLost()
        {
            if (skipConfirmModalView.RestoreSelectionIfLost())
            {
                selectionRestoredFrame = Time.frameCount;
            }
        }

        void Select(SkipConfirmResult result)
        {
            if (!IsOpen) return;
            if (Time.unscaledTime - openedTime < inputGuardSeconds) return;
            // EventSystem が Submit を処理するのは入力イベントと同じか次のフレームになるため、2 フレーム分見ておく
            if (Time.frameCount - selectionRestoredFrame <= 1) return;

            IsOpen = false;
            gameObject.SetActive(false);
            onSelected.OnNext(result);
        }

        void OnDestroy()
        {
            onSelected.Dispose();
        }
    }
}
