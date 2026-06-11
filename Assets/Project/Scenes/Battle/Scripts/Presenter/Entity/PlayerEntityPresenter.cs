using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UniRx;
using UnityEngine;
using Project.Scenes.Battle.Scripts.Model.Entity;
using Project.Scenes.Battle.Scripts.View.Entity;
using Project.Scenes.Battle.Scripts.Model;
using Project.Scenes.Battle.Scripts.Model.Movement;
using Project.Scripts.Extensions;
using Project.Scripts.Extensions.Message;
using Project.Scripts.Model;
using Project.Scripts.Presenter;

namespace Project.Scenes.Battle.Scripts.Presenter.Entity
{
    [RequireComponent(typeof(PlayerEntityView))]
    [RequireComponent(typeof(SpriteRenderer))]
    public class PlayerEntityPresenter : MonoPresenter, IEntityPresenter
    {
        [Header("Entity Settings")] [SerializeField]
        int maxHp = 100;

        [SerializeField] float moveSpeed = 5f;
        [SerializeField] float chargeThreshold = 1.0f;
        [SerializeField] float sneakSpeedMultiplier = 0.5f;
        [SerializeField] float invincibilityDuration = 1.0f;

        [Header("Shooting Settings")] [SerializeField]
        BulletPool normalBulletPool;

        [SerializeField] BulletPool chargeBulletPool;
        [SerializeField] int normalShotDamage = 10;
        [SerializeField] Vector3 chargeShotOffset = new(0, 0, 0);
        [SerializeField] int chargedShotDamage = 30;
        [SerializeField] float shootCooldown = 0.2f;

        [Header("Damage Flash")]
        [SerializeField] float damageFlashInterval = 0.1f;

        [Header("Charge Flash")]
        [SerializeField] float chargeFlashInterval = 0.08f;

        [Header("component references")] [SerializeField]
        PlayerEntityView view;

        [SerializeField] SpriteRenderer spriteRenderer;
        [SerializeField] PlayerDeathDirector deathDirector;

        void Reset()
        {
            view = GetComponent<PlayerEntityView>();
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        PlayerEntityModel model;
        float lastShootTime;
        Vector2 currentMoveInput;
        Vector2 pendingPush;
        Vector3 initialPosition;
        readonly CompositeDisposable disposables = new();
        CompositeDisposable inputDisposables;
        IDisposable damageFlashSubscription;
        IDisposable chargeFlashSubscription;

        IDisposable sceneNavigationSubscription;
        CancellationTokenSource deathCts;
        readonly Subject<Unit> deathSequenceCompleted = new();

        public PlayerEntityModel Model => model;
        public IObservable<Unit> OnDeath => model?.OnDeath;
        public IObservable<Unit> OnDeathSequenceCompleted => deathSequenceCompleted;

        void Awake()
        {
            if (normalBulletPool == null) Debug.LogError("[PlayerEntityPresenter] NormalBulletPool is not assigned!");
            if (chargeBulletPool == null) Debug.LogError("[PlayerEntityPresenter] ChargeBulletPool is not assigned!");

            sceneNavigationSubscription = MessageBroker.Default.Receive<SceneNavigationMessage>().Subscribe(OnEnteredScene).AddTo(this);

            model = new PlayerEntityModel(maxHp, chargeThreshold, sneakSpeedMultiplier, invincibilityDuration);
            initialPosition = transform.position;
            PlayerPositionReference.Transform = transform;
        }

        public void Retry()
        {
            damageFlashSubscription?.Dispose();
            damageFlashSubscription = null;
            chargeFlashSubscription?.Dispose();
            chargeFlashSubscription = null;

            // 演出途中でRetryされた場合に備えて中断
            deathCts?.Cancel();
            deathCts?.Dispose();
            deathCts = null;
            deathDirector?.ResetVisuals();

            model.Reset();
            view.ResetDamageFlash();
            view.ResetChargeFlash();
            view.EnterRun();
            view.UpdatePosition(initialPosition);
            currentMoveInput = Vector2.zero;
            pendingPush = Vector2.zero;
        }

        protected override void Start()
        {
            base.Start();
            BindModelToView();
            SubscribeToMoveInput();
            SubscribeToAttackInput();
            SubscribeToSpectrumBarPush();
        }

        void OnEnteredScene(SceneNavigationMessage message)
        {
            if (message.SceneName != SceneRouterModel.Battle) return;
            if (message.State != SceneNavigationState.Completed) return;

            sceneNavigationSubscription?.Dispose();
            Debug.Log($"[PlayerEntityPresenter] ScreenBounds initialized: ({ScreenBoundsCache.MinX}, {ScreenBoundsCache.MinY}) - ({ScreenBoundsCache.MaxX}, {ScreenBoundsCache.MaxY})");
        }

        void BindModelToView()
        {
            model.OnDeath
                 .Subscribe(_ => HandleDeath())
                 .AddTo(disposables);

            model.CurrentHp
                 .Subscribe(hp => view.SetHpRatio((float)hp / model.MaxHp))
                 .AddTo(disposables);

            model.IsInvincible
                 .Subscribe(OnInvincibleChanged)
                 .AddTo(disposables);

            model.IsChargeCompleteChanged
                 .Subscribe(OnChargeCompleteChanged)
                 .AddTo(disposables);
        }

        void OnInvincibleChanged(bool invincible)
        {
            damageFlashSubscription?.Dispose();
            damageFlashSubscription = null;

            if (!invincible)
            {
                view.ResetDamageFlash();
                // 無敵が解けた瞬間にチャージ完了が継続していれば点滅を再開
                if (model.IsChargeComplete)
                {
                    StartChargeFlash();
                }
                return;
            }

            // ダメージフラッシュ優先のため、進行中のチャージ点滅を一旦止める
            StopChargeFlash();

            // 被弾SE
            soundManager?.PlaySE(SeType.Damage);

            // 即時に1回フラッシュを開始してから周期トグル
            view.SetDamageFlashActive(true);
            bool flashOn = true;
            damageFlashSubscription = Observable
                .Interval(TimeSpan.FromSeconds(damageFlashInterval))
                .Subscribe(_ =>
                {
                    flashOn = !flashOn;
                    view.SetDamageFlashActive(flashOn);
                });
        }

        void OnChargeCompleteChanged(bool complete)
        {
            if (complete)
            {
                // 無敵中はダメージフラッシュ優先のため、チャージ点滅は始めない
                if (model.IsInvincible.Value) return;
                StartChargeFlash();
            }
            else
            {
                StopChargeFlash();
            }
        }

        void StartChargeFlash()
        {
            chargeFlashSubscription?.Dispose();
            view.SetChargeFlashActive(true);
            bool flashOn = true;
            chargeFlashSubscription = Observable
                .Interval(TimeSpan.FromSeconds(chargeFlashInterval))
                .Subscribe(_ =>
                {
                    flashOn = !flashOn;
                    view.SetChargeFlashActive(flashOn);
                });
        }

        void StopChargeFlash()
        {
            chargeFlashSubscription?.Dispose();
            chargeFlashSubscription = null;
            view.ResetChargeFlash();
        }

        void SubscribeToMoveInput()
        {
            // 移動入力を保持（Updateで使用）
            MessageBroker.Default.Receive<PlayerMoveMessage>()
                         .Subscribe(msg => { currentMoveInput = msg.value; })
                         .AddTo(disposables);
        }

        void SubscribeToSpectrumBarPush()
        {
            MessageBroker.Default.Receive<SpectrumBarPushMessage>()
                         .Subscribe(msg => { pendingPush += msg.pushDirection * msg.pushForce; })
                         .AddTo(disposables);
        }

        public void SubscribeToAttackInput()
        {
            // 既存の入力購読が残っている場合は一度破棄してから再購読する
            if (inputDisposables != null && !inputDisposables.IsDisposed)
            {
                inputDisposables.Dispose();
            }

            inputDisposables = new CompositeDisposable();

            // 攻撃ボタンをイベントで処理
            MessageBroker.Default.Receive<PlayerAttackMessage>()
                         .Where(_ => !IsPaused())
                         .Where(_ => !model.IsSneaking.Value)
                         .Where(_ => Time.time >= lastShootTime + shootCooldown)
                         .Subscribe(_ => FireNormalShot())
                         .AddTo(inputDisposables);

            // スニークボタンの押下/解除
            MessageBroker.Default.Receive<PlayerChargeMessage>()
                         .Where(_ => !IsPaused())
                         .Subscribe(msg =>
                         {
                             if (msg.isPressed)
                             {
                                 model.SetSneaking(true);
                                 view.EnterCharge();
                                 soundManager?.PlayLoopSE(SeType.Charge);
                             }
                             else
                             {
                                 soundManager?.StopLoopSE();

                                 bool chargeSucceeded = model.IsChargeComplete;
                                 if (chargeSucceeded)
                                 {
                                     FireChargedShot();
                                     model.ResetCharge();
                                 }

                                 model.SetSneaking(false);

                                 // 成功時: FireChargedShot 内の EnterAttack に任せる
                                 // 失敗時: Run に戻す
                                 if (!chargeSucceeded)
                                 {
                                     view.EnterRun();
                                 }
                             }
                         })
                         .AddTo(inputDisposables);
        }

        public void UnsubscribeFromAttackInput()
        {
            inputDisposables?.Dispose();
            inputDisposables = new CompositeDisposable();
        }

        public void CancelCharge()
        {
            // チャージ中のループSEを停止
            if (model.IsSneaking.Value)
            {
                soundManager?.StopLoopSE();
            }

            // モデル側のチャージ・スニーク状態をリセット
            model.ResetCharge();
            model.SetSneaking(false);

            // チャージ点滅を止め、見た目をRunに戻す
            StopChargeFlash();
            view.EnterRun();
        }

        bool IsPaused()
        {
            return globalScenePresenter != null
                && globalScenePresenter.PauseModalPresenter != null
                && globalScenePresenter.PauseModalPresenter.IsOpen;
        }

        public void Initialize()
        {
            view.Initialize();
        }

        public void SetColliderActive(bool active)
        {
            var col = GetComponent<Collider2D>();
            if (col != null) col.enabled = active;
        }

        public void FreezeRunAnimation()
        {
            // 走りのフレーム間隔を徐々に伸ばしてやがてStayに切り替える
            view.BeginSlowToStay();
        }

        public void UnfreezeRunAnimation()
        {
            view.EnterRun();
        }

        void Update()
        {
            if (!model.IsAlive) return;

            // 移動処理（押している間継続）+ スペクトラムバーからの押し戻し
            var movement = HandleMovement();
            movement += (Vector3)pendingPush * Time.deltaTime;
            pendingPush = Vector2.zero;
            var newPosition = ClampToScreen(view.GetPosition() + movement);
            view.UpdatePosition(newPosition);

            // チャージ処理
            if (model.IsSneaking.Value)
            {
                model.UpdateCharge(Time.deltaTime);
            }

            view.UpdateAnimation();
        }

        Vector3 HandleMovement()
        {
            if (currentMoveInput.sqrMagnitude < 0.01f) return Vector3.zero;

            var currentSpeed = model.IsSneaking.Value ? moveSpeed * model.SneakSpeedMultiplier : moveSpeed;
            var movement = new Vector3(currentMoveInput.x, currentMoveInput.y, 0) * currentSpeed * Time.deltaTime;
            return movement;
        }

        Vector3 ClampToScreen(Vector3 position)
        {
            var extents = spriteRenderer.bounds.extents;

            var minWorld = new Vector3(ScreenBoundsCache.MinX, ScreenBoundsCache.MinY);
            var maxWorld = new Vector3(ScreenBoundsCache.MaxX, ScreenBoundsCache.MaxY);

            position.x = Mathf.Clamp(position.x, minWorld.x + extents.x, maxWorld.x - extents.x);
            position.y = Mathf.Clamp(position.y, minWorld.y + extents.y - 0.2f, maxWorld.y - extents.y - 2.1f);

            return position;
        }

        void FireNormalShot()
        {
            normalBulletPool.SpawnBullet(normalShotDamage, transform.position);
            lastShootTime = Time.time;
            view.EnterAttack();
            soundManager?.PlaySE(SeType.Attack);
        }

        void FireChargedShot()
        {
            chargeBulletPool.SpawnBullet(chargedShotDamage, transform.position + chargeShotOffset, isPlayerBullet: true);
            view.EnterAttack();
            soundManager?.PlaySE(SeType.ChargeRelease);
        }

        void HandleDeath()
        {
            UnsubscribeFromAttackInput();
            SetColliderActive(false);

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
                return;
            }

            deathSequenceCompleted.OnNext(Unit.Default);
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
            if (PlayerPositionReference.Transform == transform)
            {
                PlayerPositionReference.Transform = null;
            }

            deathCts?.Cancel();
            deathCts?.Dispose();
            deathCts = null;

            damageFlashSubscription?.Dispose();
            chargeFlashSubscription?.Dispose();
            disposables.Dispose();
            inputDisposables?.Dispose();
            deathSequenceCompleted.Dispose();
            model?.Dispose();
        }

        public EntityBase GetModel()
        {
            return model;
        }
    }
}
