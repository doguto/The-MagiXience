using System;
using UniRx;
using UnityEngine;
using UnityEngine.Pool;
using Project.Scenes.Battle.Scripts.Model.Entity;
using Project.Scenes.Battle.Scripts.Model.Movement;
using Project.Scenes.Battle.Scripts.View.Entity;

namespace Project.Scenes.Battle.Scripts.Presenter.Entity
{
    [RequireComponent(typeof(BulletEntityView))]
    public class BulletEntityPresenter : MonoBehaviour, IEntityPresenter
    {
        [SerializeReference, SubclassSelector]
        IMovementConfig movementConfig = new LinearMovementConfig();

        // 同Objectにアタッチされている想定だが、負荷軽減のためSerializeFieldで
        [SerializeField] BulletEntityView view;
        [SerializeField] SpriteRenderer spriteRenderer;
        
        BulletEntityModel model;
        IObjectPool<BulletEntityPresenter> pool;
        Camera mainCamera;
        readonly CompositeDisposable disposables = new();

        public BulletEntityModel Model => model;

        IMovementStrategy CreateMovementStrategy()
        {
            return movementConfig?.CreateStrategy() ?? new StaticMovement();
        }

        void Awake()
        {
            mainCamera = Camera.main;
        }

        public void Initialize(int damage, Vector3 position, IObjectPool<BulletEntityPresenter> objectPool)
        {
            pool = objectPool;
            var movementStrategy = CreateMovementStrategy();

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
            view.SetVisible(false);
            gameObject.SetActive(false);
        }

        public void OnTakenFromPool()
        {
            gameObject.SetActive(true);
        }

        void OnDestroy()
        {
            disposables.Dispose();
            model?.Dispose();
        }

        public EntityBase GetModel() => model;
    }
}
