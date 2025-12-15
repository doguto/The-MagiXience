using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Project.Commons.UI.Scripts.View
{
    [RequireComponent(typeof(Collider2D))]
    public class SimpleButton : ButtonBase, IDisposable
    {
        [SerializeField] SpriteRenderer spriteRenderer;

        [SerializeField] float brightnessRatioOnPressed = 0.85f;
        [SerializeField] float brightnessDurationOnPressed = 0.20f;
        [SerializeField] float scaleChangeRatio = 1.05f;
        [SerializeField] float brightnessRatioOnClose = 0.7f;

        Transform myTransform;
        Color originalColor;
        Vector3 originalLocalScale;

        readonly CancellationTokenSource cancellationTokenSource = new();

        protected override void Awake()
        {
            myTransform = transform;
            originalColor = spriteRenderer.color;
            originalLocalScale = myTransform.localScale;
        }

        public void Dispose()
        {
            onPressed.Dispose();
            cancellationTokenSource.Cancel();
            cancellationTokenSource.Dispose();
        }

        protected override void OnFocused()
        {
            base.OnFocused();
            myTransform.localScale = originalLocalScale * scaleChangeRatio;
        }

        public override void OnSubmit(BaseEventData eventData)
        {
            DarkenColor(cancellationTokenSource.Token).Forget();
            base.OnSubmit(eventData);
        }

        protected override void OnUnfocused()
        {
            base.OnUnfocused();
            myTransform.localScale = originalLocalScale;
        }

        public override void OnPointerDown(PointerEventData eventData)
        {
            base.OnPointerDown(eventData);
            spriteRenderer.color = originalColor * brightnessRatioOnPressed;
        }

        public override void OnPointerUp(PointerEventData eventData)
        {
            base.OnPointerUp(eventData);
            spriteRenderer.color = originalColor;
        }

        protected override void OnOpened()
        {
            base.OnOpened();
            spriteRenderer.color = originalColor;
        }

        protected override void OnClosed()
        {
            base.OnClosed();
            spriteRenderer.color = originalColor * brightnessRatioOnClose;
        }

        async UniTask DarkenColor(CancellationToken token)
        {
            spriteRenderer.color = originalColor * brightnessRatioOnPressed;
            await UniTask.WaitForSeconds(
                brightnessDurationOnPressed,
                cancellationToken: token
            );
            spriteRenderer.color = originalColor;
        }
    }
}
