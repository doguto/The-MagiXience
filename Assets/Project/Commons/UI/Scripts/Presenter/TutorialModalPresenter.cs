using System;
using Project.Commons.UI.Scripts.View;
using Project.Scripts.Extensions.Message;
using Project.Scripts.Presenter;
using UniRx;
using UnityEngine;

namespace Project.Commons.UI.Scripts.Presenter
{
    /// <summary>
    /// 1面道中開始時に操作方法を提示するチュートリアルモーダル。
    /// UISubmit もしくは PlayerAttack 入力で閉じ、閉じたことを OnClosed で通知する。
    /// timeScale は操作しない（呼び出し側がシーケンス開始を遅延させる前提）。
    /// </summary>
    public class TutorialModalPresenter : MonoPresenter
    {
        [SerializeField] TutorialModalView tutorialModalView;

        readonly Subject<Unit> onClosed = new();
        public IObservable<Unit> OnClosed => onClosed;
        public bool IsOpen { get; private set; }

        IDisposable closeInputSubscription;

        protected override void Start()
        {
            base.Start();

            // Globalシーンで起動された時点では非表示にしておく
            if (!IsOpen)
            {
                tutorialModalView.Hide();
            }
        }

        public void Open()
        {
            if (IsOpen) return;
            IsOpen = true;

            tutorialModalView.Show();

            // UISubmit または PlayerAttack のどちらかで閉じる
            closeInputSubscription?.Dispose();
            closeInputSubscription = Observable.Merge(
                    MessageBroker.Default.Receive<UISubmitMessage>().AsUnitObservable(),
                    MessageBroker.Default.Receive<PlayerAttackMessage>().AsUnitObservable())
                .Subscribe(_ => Close());
        }

        public void Close()
        {
            if (!IsOpen) return;
            IsOpen = false;

            closeInputSubscription?.Dispose();
            closeInputSubscription = null;

            tutorialModalView.Hide();
            onClosed.OnNext(Unit.Default);
        }

        void OnDestroy()
        {
            closeInputSubscription?.Dispose();
            closeInputSubscription = null;
            onClosed.Dispose();
        }
    }
}
