using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using Project.Scenes.Battle.Scripts.Presenter;

namespace Project.Scenes.Battle.Scripts.Presenter.Entity
{
    public class BossDeathDirector : DeathDirectorBase
    {
        [Header("Boss Specific")] 
        [SerializeField] Transform targetTransform;
        [SerializeField] float scaleMultiplier = 1.4f;

        BulletClearReceiver bulletClearReceiver;
        
        void Reset()
        {
            targetTransform = transform;
        }
        void Awake()
        {
            if (bulletClearReceiver == null)
            {
                bulletClearReceiver = FindFirstObjectByType<BulletClearReceiver>();
            }
        }

        protected override void OnBeforeHitStop()
        {
            bulletClearReceiver?.ClearAllBullets();
            bulletClearReceiver?.ClearAllEnemies();
        }

        protected override async UniTask PlayMainSequenceAsync(CancellationToken ct)
        {
            var seq = DOTween.Sequence();
            if (targetTransform != null)
            {
                _ = seq.Append(targetTransform
                    .DOScale(targetTransform.localScale * scaleMultiplier, fadeDuration)
                    .SetEase(Ease.OutQuad));
            }
            if (targetSpriteRenderer != null)
            {
                _ = seq.Join(targetSpriteRenderer.DOFade(0f, fadeDuration));
            }
            await seq.ToUniTask(cancellationToken: ct);
        }
    }
}
