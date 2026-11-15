using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UniRx;
using UnityEngine;
using Project.Scenes.Battle.Scripts.Model.Entity;
using Project.Scenes.Battle.Scripts.View.Entity;
using Project.Scripts.Presenter;

namespace Project.Scenes.Battle.Scripts.Presenter.Entity
{
    // ボスの HP・被弾・撃破演出を受け持つ。攻撃と移動は BattleActor が実行する BehaviorTree が行う
    [RequireComponent(typeof(BossEntityView))]
    public class BossEntityPresenter : MonoPresenter, IEntityPresenter
    {
        [Header("Entity Settings")]
        [SerializeField] int maxHp = 200;
        [SerializeField] int contactDamage = 20;
        [SerializeField, Range(0f, 1f)] float strongHpRatio = 0.5f;
        [SerializeField, Range(0f, 1f)] float overflowDamageMultiplier = 0f;
        [Tooltip("シナリオ→ボス戦突入直後、ボスのColliderを無効化して接触ダメージを防ぐ秒数")]
        [SerializeField, Min(0f)] float entryColliderDisableSeconds = 1f;

        [Header("Damage Flash")]
        [SerializeField] float damageFlashInterval = 0.05f;
        [SerializeField] float damageFlashDuration = 0.2f;

        [Header("Component References")]
        [SerializeField] BossEntityView view;
        [SerializeField] BossDeathDirector deathDirector;

        void Reset()
        {
            view = GetComponent<BossEntityView>();
        }

        BossEntityModel model;
        CancellationTokenSource deathCts;
        CancellationTokenSource aliveCts;
        readonly CompositeDisposable disposables = new();
        IDisposable damageFlashSubscription;
        readonly Subject<Unit> deathSequenceCompleted = new();

        public BossEntityModel Model => model;
        public IObservable<Unit> OnDeath => model?.OnDeath;
        public IObservable<Unit> OnDeathSequenceCompleted => deathSequenceCompleted;

        void Awake()
        {
            aliveCts = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);
            model = new BossEntityModel(maxHp, contactDamage, strongHpRatio, overflowDamageMultiplier);

            model.OnDeath
                .Subscribe(_ => HandleDeath())
                .AddTo(disposables);

            SubscribeToDamageFlash();
            SubscribeToHpBar();

            view.UpdatePosition(transform.position);
        }

        void SubscribeToHpBar()
        {
            float denom = model.MaxHp > 0 ? model.MaxHp : 1f;

            model.CurrentHp
                .Subscribe(hp => view.SetHpRatio(hp / denom))
                .AddTo(disposables);
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

        /// <summary>
        /// ボス戦突入直後の接触ダメージを防ぐため、一定時間ボスのColliderを無効化する。
        /// 無効化中はプレイヤーの弾も当たらない。撃破・破棄された場合は再有効化しない。
        /// </summary>
        public void DisableColliderOnEntry()
        {
            if (entryColliderDisableSeconds <= 0f) return;
            var col = GetComponent<Collider2D>();
            if (col == null) return;
            DisableColliderTemporarilyAsync(col, entryColliderDisableSeconds, aliveCts.Token).Forget();
        }

        async UniTaskVoid DisableColliderTemporarilyAsync(Collider2D col, float seconds, CancellationToken ct)
        {
            col.enabled = false;
            if (await UniTask.Delay(TimeSpan.FromSeconds(seconds), cancellationToken: ct).SuppressCancellationThrow()) return;
            if (col == null || model == null || !model.IsAlive) return;
            col.enabled = true;
        }

        void HandleDeath()
        {
            aliveCts?.Cancel();
            var col = GetComponent<Collider2D>();
            if (col != null) col.enabled = false;

            deathCts?.Cancel();
            deathCts?.Dispose();
            deathCts = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);
            PlayDeathSequenceAsync(deathCts.Token).Forget();
        }

        async UniTaskVoid PlayDeathSequenceAsync(CancellationToken ct)
        {
            try
            {
                if (deathDirector != null)
                {
                    await deathDirector.PlayAsync(ct);
                }
            }
            catch (OperationCanceledException)
            {
                // Retry等で中断: そのまま終わる
                return;
            }

            deathSequenceCompleted.OnNext(Unit.Default);
            Destroy(gameObject);
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
            deathCts?.Cancel();
            deathCts?.Dispose();
            deathCts = null;
            aliveCts?.Cancel();
            aliveCts?.Dispose();
            aliveCts = null;
            damageFlashSubscription?.Dispose();
            disposables.Dispose();
            deathSequenceCompleted.Dispose();
            model?.Dispose();
        }

        public EntityBase GetModel() => model;
    }
}
