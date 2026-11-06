using System;
using Project.Scenes.Scenario.Scripts.View;
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

        void Awake()
        {
            skipConfirmModalView.OnPressedEnableMashSkip.Subscribe(_ => Select(SkipConfirmResult.EnableMashSkip)).AddTo(this);
            skipConfirmModalView.OnPressedSkipOnce.Subscribe(_ => Select(SkipConfirmResult.SkipOnce)).AddTo(this);
            skipConfirmModalView.OnPressedCancel.Subscribe(_ => Select(SkipConfirmResult.Cancel)).AddTo(this);

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

        void Select(SkipConfirmResult result)
        {
            if (!IsOpen) return;
            if (Time.unscaledTime - openedTime < inputGuardSeconds) return;

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
