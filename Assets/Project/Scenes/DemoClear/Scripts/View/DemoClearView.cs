using System;
using Project.Scripts.Extensions.Message;
using UniRx;
using UnityEngine;

namespace Project.Scenes.DemoClear.Scripts.View
{
    public class DemoClearView : MonoBehaviour
    {
        [SerializeField] CanvasGroup canvasGroup;
        [SerializeField] float fadeInDuration = 1f;

        readonly Subject<Unit> onAnyKeyPressed = new();
        public IObservable<Unit> OnAnyKeyPressed => onAnyKeyPressed;

        bool acceptInput;
        readonly CompositeDisposable disposables = new();

        void Start()
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
            }

            // UISubmit または PlayerAttack でタイトルに戻る
            MessageBroker.Default.Receive<UISubmitMessage>()
                .Where(_ => acceptInput)
                .Subscribe(_ => onAnyKeyPressed.OnNext(Unit.Default))
                .AddTo(disposables);

            MessageBroker.Default.Receive<PlayerAttackMessage>()
                .Where(_ => acceptInput)
                .Subscribe(_ => onAnyKeyPressed.OnNext(Unit.Default))
                .AddTo(disposables);
        }

        public void Show()
        {
            acceptInput = false;
            StartCoroutine(FadeIn());
        }

        System.Collections.IEnumerator FadeIn()
        {
            if (canvasGroup == null)
            {
                acceptInput = true;
                yield break;
            }

            var elapsed = 0f;
            while (elapsed < fadeInDuration)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = Mathf.Clamp01(elapsed / fadeInDuration);
                yield return null;
            }

            canvasGroup.alpha = 1f;
            acceptInput = true;
        }

        void OnDestroy()
        {
            disposables.Dispose();
            onAnyKeyPressed.Dispose();
        }
    }
}
