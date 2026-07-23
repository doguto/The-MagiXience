using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UniRx;
using UnityEngine;
using UnityEngine.Pool;
using Project.Scenes.Battle.Scripts.Model;
using Project.Scenes.Battle.Scripts.Model.Entity;
using Project.Scenes.Battle.Scripts.Model.Movement;
using Project.Scenes.Battle.Scripts.View.Entity;

namespace Project.Scenes.Battle.Scripts.Presenter.Entity
{
    [RequireComponent(typeof(BulletEntityView))]
    [RequireComponent(typeof(SpriteRenderer))]
    public class BulletEntityPresenter : MonoBehaviour, IEntityPresenter
    {
        [SerializeReference, SubclassSelector]
        List<IMovementStep> movementSteps = new() { new InfiniteMovementConfig() };

        [SerializeField] BulletEntityView view;
        [SerializeField] SpriteRenderer spriteRenderer;
        [SerializeField, Tooltip("弾のライフタイム（秒）。0以下で無制限")]
        float lifetime = 5f;

        void Reset()
        {
            view = GetComponent<BulletEntityView>();
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        BulletEntityModel model;
        IObjectPool<BulletEntityPresenter> pool;
        Tween currentTween;
        CancellationTokenSource movementCts;
        CancellationTokenSource lifetimeCts;
        readonly CompositeDisposable disposables = new();

        public BulletEntityModel Model => model;

        public void Initialize(int damage, Vector3 position, Vector2 direction, IObjectPool<BulletEntityPresenter> objectPool, bool isPlayerBullet = false, Quaternion rotation = default)
        {
            pool = objectPool;
            var resolvedRotation = rotation == default ? Quaternion.identity : rotation;
            transform.SetPositionAndRotation(position, resolvedRotation);

            if (model == null)
                model = new BulletEntityModel(damage, isPlayerBullet);
            else
                model.Reinitialize(damage, isPlayerBullet);

            StartMovementSequence(direction);

            view.ResetView();
            view.UpdatePosition(position);

            BindModelToView();
        }

        void StartMovementSequence(Vector2 direction)
        {
            StopMovement();

            if (movementSteps == null || movementSteps.Count == 0) return;

            movementCts = new CancellationTokenSource();
            RunMovementStepsAsync(direction, movementCts.Token).Forget();
        }

        async UniTaskVoid RunMovementStepsAsync(Vector2 direction, CancellationToken ct)
        {
            foreach (var step in movementSteps)
            {
                if (step == null) continue;
                ct.ThrowIfCancellationRequested();

                bool grantsInvincibility = step is IInvincibilityGrantingStep;
                if (grantsInvincibility) model.SetInvincible(true);

                try
                {
                    currentTween = step.Play(transform, direction, null);
                    await currentTween.ToUniTask(TweenCancelBehaviour.KillAndCancelAwait, ct);
                }
                finally
                {
                    if (grantsInvincibility) model.SetInvincible(false);
                }
            }
        }

        void StopMovement()
        {
            movementCts?.Cancel();
            movementCts?.Dispose();
            movementCts = null;
            currentTween = null;
        }

        void BindModelToView()
        {
            disposables.Clear();

            model.CurrentHp
                .Where(hp => hp <= 0)
                .Subscribe(_ => HandleDestruction())
                .AddTo(disposables);

            StartLifetimeTimer();
        }

        void Update()
        {
            if (model == null || !model.IsAlive) return;

            view.UpdatePosition(transform.position);

            if (IsOutOfScreen())
            {
                ReturnToPool();
            }
        }

        bool IsOutOfScreen()
        {
            var position = transform.position;
            var extents = spriteRenderer.bounds.extents;
            var margin = Mathf.Max(extents.x, extents.y) + 0.1f;

            return position.x < ScreenBoundsCache.MinX - margin || position.x > ScreenBoundsCache.MaxX + margin ||
                   position.y < ScreenBoundsCache.MinY - margin || position.y > ScreenBoundsCache.MaxY + margin;
        }

        void StartLifetimeTimer()
        {
            CancelLifetimeTimer();
            if (lifetime <= 0f) return;

            lifetimeCts = new CancellationTokenSource();
            LifetimeTimerAsync(lifetimeCts.Token).Forget();
        }

        async UniTaskVoid LifetimeTimerAsync(CancellationToken ct)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(lifetime), cancellationToken: ct);
            ReturnToPool();
        }

        void CancelLifetimeTimer()
        {
            lifetimeCts?.Cancel();
            lifetimeCts?.Dispose();
            lifetimeCts = null;
        }

        void HandleDestruction() => ReturnToPool();

        public void ReturnToPool()
        {
            StopMovement();
            if (pool != null)
                pool.Release(this);
            else
                Destroy(gameObject);
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (model == null || !model.IsAlive) return;
            var otherPresenter = other.GetComponent<IEntityPresenter>();
            if (otherPresenter != null)
                model.OnCollision(otherPresenter.GetModel());
        }

        public void OnReturnedToPool()
        {
            CancelLifetimeTimer();
            StopMovement();
            view.SetVisible(false);
            gameObject.SetActive(false);
        }

        public void OnTakenFromPool()
        {
            gameObject.SetActive(true);
        }

        void OnDestroy()
        {
            CancelLifetimeTimer();
            StopMovement();
            disposables.Dispose();
            model?.Dispose();
        }

        public EntityBase GetModel() => model;
    }
}
