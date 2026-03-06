using System;
using System.Threading;
using Cysharp.Threading.Tasks;
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
        IMovementConfig movementConfig = new LinearMovementConfig();

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
        CancellationTokenSource lifetimeCts;
        readonly CompositeDisposable disposables = new();

        public BulletEntityModel Model => model;

        IMovementStrategy CreateMovementStrategy(Vector2 direction)
        {
            if (movementConfig == null) return new StaticMovement();
            return direction != Vector2.zero
                ? movementConfig.CreateStrategy(direction)
                : movementConfig.CreateStrategy();
        }

        void Awake()
        {
            mainCamera = Camera.main;
        }

        public void Initialize(int damage, Vector3 position, Vector2 direction, IObjectPool<BulletEntityPresenter> objectPool)
        {
            pool = objectPool;
            var movementStrategy = CreateMovementStrategy(direction);

            if (model == null)
            {
                model = new BulletEntityModel(damage, position, movementStrategy);
            }
            else
            {
                model.Reinitialize(damage, position, movementStrategy);
            }

            view.ResetView();
            view.UpdatePosition(position);

            BindModelToView();
        }

        void BindModelToView()
        {
            // 既存のサブスクリプションをクリア
            disposables.Clear();

            // HP変化を監視（弾が死んだら破棄）
            model.CurrentHp
                .Where(hp => hp <= 0)
                .Subscribe(_ => HandleDestruction())
                .AddTo(disposables);

            // ライフタイムで自動回収
            StartLifetimeTimer();
        }

        void Update()
        {
            if (model == null || !model.IsAlive) return;

            // MovementStrategyで移動処理
            model.UpdateMovement(Time.deltaTime);

            // 位置をViewに反映
            view.UpdatePosition(model.Position);

            // 画面外判定（簡易実装）
            if (IsOutOfScreen())
            {
                ReturnToPool();
            }
        }

        bool IsOutOfScreen()
        {
            Vector3 position = model.Position;
            Vector3 viewportPoint = mainCamera.WorldToViewportPoint(position);

            // Spriteサイズ分のマージンをビューポート座標に変換
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

        void HandleDestruction()
        {
            ReturnToPool();
        }

        void ReturnToPool()
        {
            if (pool != null)
            {
                pool.Release(this);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (model == null || !model.IsAlive) return;

            var otherPresenter = other.GetComponent<IEntityPresenter>();
            if (otherPresenter != null)
            {
                model.OnCollision(otherPresenter.GetModel());
            }
        }
        
        public void OnReturnedToPool()
        {
            CancelLifetimeTimer();
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
            disposables.Dispose();
            model?.Dispose();
        }

        public EntityBase GetModel() => model;
    }
}
