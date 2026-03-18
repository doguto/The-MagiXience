using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UniRx;
using UnityEngine;
using UnityEngine.Pool;
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
        Camera mainCamera;
        Sequence movementSequence;
        CancellationTokenSource lifetimeCts;
        readonly CompositeDisposable disposables = new();

        public BulletEntityModel Model => model;

        void Awake()
        {
            mainCamera = Camera.main;
        }

        public void Initialize(int damage, Vector3 position, Vector2 direction, IObjectPool<BulletEntityPresenter> objectPool, bool isPlayerBullet = false)
        {
            pool = objectPool;
            transform.position = position;

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
            movementSequence?.Kill();

            if (movementSteps == null || movementSteps.Count == 0) return;

            movementSequence = DOTween.Sequence();
            foreach (var step in movementSteps)
            {
                if (step == null) continue;
                movementSequence.Append(step.Play(transform, direction, null));
            }
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
            Vector3 position = transform.position;
            Vector3 viewportPoint = mainCamera.WorldToViewportPoint(position);

            Vector3 extents = spriteRenderer.bounds.extents;
            Vector3 viewportExtents = mainCamera.WorldToViewportPoint(position + extents)
                                    - mainCamera.WorldToViewportPoint(position);
            float margin = Mathf.Max(Mathf.Abs(viewportExtents.x), Mathf.Abs(viewportExtents.y)) + 0.1f;

            return viewportPoint.x < -margin || viewportPoint.x > 1f + margin ||
                   viewportPoint.y < -margin || viewportPoint.y > 1f + margin;
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

        void ReturnToPool()
        {
            movementSequence?.Kill();
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
            movementSequence?.Kill();
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
            movementSequence?.Kill();
            disposables.Dispose();
            model?.Dispose();
        }

        public EntityBase GetModel() => model;
    }
}
