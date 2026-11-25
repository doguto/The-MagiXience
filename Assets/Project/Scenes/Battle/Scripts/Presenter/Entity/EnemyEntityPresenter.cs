using System;
using UniRx;
using UnityEngine;
using Project.Scenes.Battle.Scripts.Model;
using Project.Scenes.Battle.Scripts.Model.Entity;
using Project.Scenes.Battle.Scripts.Runtime;
using Project.Scenes.Battle.Scripts.View.Entity;
using Project.Scripts.Extensions;
using Project.Scripts.Presenter;

namespace Project.Scenes.Battle.Scripts.Presenter.Entity
{
    // 敵の HP・被弾・見た目を受け持つ。移動・攻撃・消えるかの判定は EnemyAgent が行う
    [RequireComponent(typeof(EnemyEntityView))]
    [RequireComponent(typeof(SpriteRenderer))]
    public class EnemyEntityPresenter : MonoPresenter, IEntityPresenter, IPoolableEnemy
    {
        [Header("Entity Settings")] [SerializeField]
        int maxHp = 50;

        [SerializeField] int contactDamage = 10;
        [Tooltip("ONにすると通常攻撃は無効（被ダメージ0）、チャージ攻撃のみ有効")]
        [SerializeField] bool onlyChargeDamageable = false;
        [SerializeField, Tooltip("秒。0 以下で無制限")] float lifetime = 0f;

        [Header("Damage Flash")]
        [SerializeField] float damageFlashInterval = 0.05f;
        [SerializeField] float damageFlashDuration = 0.2f;

        [Header("component references")] [SerializeField]
        EnemyEntityView view;

        [SerializeField] SpriteRenderer spriteRenderer;

        void Reset()
        {
            view = GetComponent<EnemyEntityView>();
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        EnemyEntityModel model;
        readonly CompositeDisposable disposables = new();
        IDisposable damageFlashSubscription;
        readonly Subject<Unit> removed = new();
        bool removedNotified;

        public EnemyEntityModel Model => model;
        public float LifetimeSeconds => lifetime;
        // 画面外とみなすまでの余白
        public float ScreenMargin
        {
            get
            {
                var extents = spriteRenderer.bounds.extents;
                return Mathf.Max(extents.x, extents.y) + 0.1f;
            }
        }
        public IObservable<Unit> OnDeath => model?.OnDeath;
        // 場から消えたとき (撃破・画面外・寿命・プールへの返却・破棄) に 1 回だけ通知する
        public IObservable<Unit> OnRemoved => removed;

        void Awake()
        {
            Initialize(transform.position);
        }

        public void Initialize(Vector3 spawnPosition)
        {
            transform.position = spawnPosition;
            model = new EnemyEntityModel(maxHp, contactDamage, onlyChargeDamageable);
            SetCollidersEnabled(true);
            removedNotified = false;

            model.OnDeath
                 .Subscribe(_ => HandleDeath())
                 .AddTo(disposables);

            model.OnIneffectiveHit
                 .TakeUntil(model.OnDeath)
                 .Subscribe(_ => soundManager?.PlaySE(SeType.Metal))
                 .AddTo(disposables);

            SubscribeToDamageFlash();

            view.UpdatePosition(transform.position);
        }

        void SubscribeToDamageFlash()
        {
            int previousHp = model.CurrentHp.Value;
            model.CurrentHp
                 .Skip(1)
                 .Subscribe(hp =>
                 {
                     if (hp < previousHp)
                     {
                         PlayDamageFlash();
                     }
                     previousHp = hp;
                 })
                 .AddTo(disposables);
        }

        void PlayDamageFlash()
        {
            damageFlashSubscription?.Dispose();

            view.SetDamageFlashActive(true);
            bool flashOn = true;
            float elapsed = 0f;

            damageFlashSubscription = Observable
                .Interval(TimeSpan.FromSeconds(damageFlashInterval))
                .Subscribe(_ =>
                {
                    elapsed += damageFlashInterval;
                    if (elapsed >= damageFlashDuration)
                    {
                        view.ResetDamageFlash();
                        damageFlashSubscription?.Dispose();
                        damageFlashSubscription = null;
                        return;
                    }
                    flashOn = !flashOn;
                    view.SetDamageFlashActive(flashOn);
                });
        }

        // 片付けは EnemyAgent が次のフレームに行う。それまでに他のものへ当たらないようにする
        void HandleDeath() => SetCollidersEnabled(false);

        void SetCollidersEnabled(bool value)
        {
            foreach (var c in GetComponents<Collider2D>()) c.enabled = value;
        }

        void NotifyRemoved()
        {
            if (removedNotified) return;
            removedNotified = true;
            removed.OnNext(Unit.Default);
        }

        public void OnTakenFromPool()
        {
            Initialize(transform.position);
        }

        public void OnReturnedToPool()
        {
            NotifyRemoved();
            CleanupLife();
        }

        // 1 体分の生存中に作ったもの (購読・寿命・点滅・Model) を片付ける
        void CleanupLife()
        {
            damageFlashSubscription?.Dispose();
            damageFlashSubscription = null;
            view.ResetDamageFlash();
            disposables.Clear();
            model?.Dispose();
            model = null;
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            var otherPresenter = other.GetComponent<IEntityPresenter>();
            if (otherPresenter != null)
            {
                model.OnCollision(otherPresenter.GetModel());
            }
        }

        void OnDestroy()
        {
            NotifyRemoved();
            damageFlashSubscription?.Dispose();
            damageFlashSubscription = null;
            disposables.Dispose();
            model?.Dispose();
            removed.Dispose();
        }

        public EntityBase GetModel()
        {
            return model;
        }
    }
}
