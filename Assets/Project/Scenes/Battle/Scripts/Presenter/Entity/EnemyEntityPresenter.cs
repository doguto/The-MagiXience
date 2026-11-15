using System;
using System.Threading;
using Cysharp.Threading.Tasks;
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
    // 敵の HP・被弾・寿命・画面外判定を受け持つ。移動と攻撃は BattleActor が実行する BehaviorTree が行う
    [RequireComponent(typeof(EnemyEntityView))]
    [RequireComponent(typeof(SpriteRenderer))]
    public class EnemyEntityPresenter : MonoPresenter, IEntityPresenter, IPoolableActor
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
        CancellationTokenSource lifetimeCts;
        readonly CompositeDisposable disposables = new();
        IDisposable damageFlashSubscription;
        bool isEnteredScreen;
        readonly Subject<Unit> removed = new();
        bool removedNotified;

        public EnemyEntityModel Model => model;
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
            isEnteredScreen = false;
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

            StartLifetimeCountdown();
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

        void StartLifetimeCountdown()
        {
            if (lifetime <= 0f) return;

            lifetimeCts = new CancellationTokenSource();
            WaitLifetimeAsync(lifetimeCts.Token).Forget();
        }

        async UniTaskVoid WaitLifetimeAsync(CancellationToken ct)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(lifetime), cancellationToken: ct);
            ActorDespawn.Despawn(gameObject);
        }

        void Update()
        {
            if (model == null || !model.IsAlive) return;

            if (IsOutOfScreen())
            {
                ActorDespawn.Despawn(gameObject);
            }
        }

        bool IsOutOfScreen()
        {
            var position = transform.position;
            var extents = spriteRenderer.bounds.extents;
            var margin = Mathf.Max(extents.x, extents.y) + 0.1f;

            var outOfScreen = position.x < ScreenBoundsCache.MinX - margin || position.x > ScreenBoundsCache.MaxX + margin ||
                              position.y < ScreenBoundsCache.MinY - margin || position.y > ScreenBoundsCache.MaxY + margin;

            if (!outOfScreen) isEnteredScreen = true;

            return isEnteredScreen && outOfScreen;
        }

        void HandleDeath()
        {
            ActorDespawn.Despawn(gameObject);
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
            lifetimeCts?.Cancel();
            lifetimeCts?.Dispose();
            lifetimeCts = null;
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
            lifetimeCts?.Cancel();
            lifetimeCts?.Dispose();
            lifetimeCts = null;
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
