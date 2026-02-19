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
        BulletEntityView view;
        BulletEntityModel model;
        IObjectPool<BulletEntityPresenter> pool;
        readonly CompositeDisposable disposables = new();

        public BulletEntityModel Model => model;

        void Awake()
        {
            view = GetComponent<BulletEntityView>();
        }

        public void Initialize(int damage, Vector3 position, IMovementStrategy movementStrategy, bool isFriendly, IObjectPool<BulletEntityPresenter> objectPool)
        {
            pool = objectPool;

            if (model == null)
            {
                model = new BulletEntityModel(damage, position, movementStrategy, isFriendly);
            }
            else
            {
                model.Reinitialize(damage, position, movementStrategy, isFriendly);
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
            // カメラの視野外に出たらtrue
            Vector3 screenPoint = Camera.main.WorldToViewportPoint(model.Position);
            return screenPoint.x < -0.1f || screenPoint.x > 1.1f ||
                   screenPoint.y < -0.1f || screenPoint.y > 1.1f;
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
