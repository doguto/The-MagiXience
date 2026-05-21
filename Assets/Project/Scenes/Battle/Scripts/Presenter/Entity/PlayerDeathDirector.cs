using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Presenter.Entity
{
    public class PlayerDeathDirector : DeathDirectorBase
    {
        Color originalColor;
        Vector3 originalScale;
        bool capturedOriginals;

        void Awake()
        {
            CaptureOriginals();
        }

        void CaptureOriginals()
        {
            if (capturedOriginals) return;
            if (targetSpriteRenderer != null)
            {
                originalColor = targetSpriteRenderer.color;
            }
            originalScale = transform.localScale;
            capturedOriginals = true;
        }

        protected override async UniTask PlayMainSequenceAsync(CancellationToken ct)
        {
            if (targetSpriteRenderer == null) return;
            await targetSpriteRenderer.DOFade(0f, fadeDuration).ToUniTask(cancellationToken: ct);
        }

        /// <summary>
        /// Retry時に呼ぶ。スプライト色とスケールを初期状態に戻す。
        /// </summary>
        public void ResetVisuals()
        {
            if (!capturedOriginals) return;
            if (targetSpriteRenderer != null)
            {
                targetSpriteRenderer.color = originalColor;
            }
            transform.localScale = originalScale;
        }
    }
}
