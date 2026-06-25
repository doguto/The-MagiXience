using System;
using System.Threading;
using Cysharp.Threading.Tasks;
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
        [SerializeField] float skipLockDuration = 3f;

        readonly Subject<Unit> onClosed = new();
        public IObservable<Unit> OnClosed => onClosed;
        public bool IsOpen { get; private set; }

        CancellationTokenSource closeCts;

        protected override void Start()
        {
            base.Start();

            // Globalシーンで起動された時点では非表示にしておく
            if (!IsOpen)
            {
                tutorialModalView.Hide();
            }
        }

        public void Open(bool isFirstEntry)
        {
            if (IsOpen) return;
            IsOpen = true;

            tutorialModalView.Show();

            closeCts = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());
            WaitForCloseAsync(isFirstEntry, closeCts.Token).Forget();
        }

        async UniTaskVoid WaitForCloseAsync(bool isFirstEntry, CancellationToken token)
        {
            // 開いてから lockDuration 秒間はスキップ不可
            var lockDuration = isFirstEntry ? skipLockDuration : 0f;
            await UniTask.Delay(TimeSpan.FromSeconds(lockDuration), cancellationToken: token);

            // 待機時間が過ぎたら「Push Z to start」テキストを表示する
            tutorialModalView.SetPushZToStartActive(true);

            // UISubmit または PlayerAttack のどちらかが来たら閉じる
            await UniTask.WhenAny(
                MessageBroker.Default.Receive<UISubmitMessage>().ToUniTask(useFirstValue: true, cancellationToken: token),
                MessageBroker.Default.Receive<PlayerAttackMessage>().ToUniTask(useFirstValue: true, cancellationToken: token));

            Close();
        }

        public void Close()
        {
            if (!IsOpen) return;
            IsOpen = false;

            closeCts?.Cancel();
            closeCts?.Dispose();
            closeCts = null;

            tutorialModalView.Hide();
            onClosed.OnNext(Unit.Default);
        }

        void OnDestroy()
        {
            closeCts?.Cancel();
            closeCts?.Dispose();
            closeCts = null;
            onClosed.Dispose();
        }
    }
}
