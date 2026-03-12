using System;
using UniRx;
using UnityEngine;
using Project.Scenes.Battle.Scripts.Model.Entity;
using Project.Scenes.Battle.Scripts.View.Entity;
using Project.Scripts.Extensions.Message;

namespace Project.Scenes.Battle.Scripts.Presenter.Entity
{
    [RequireComponent(typeof(PlayerEntityView))]
    [RequireComponent(typeof(SpriteRenderer))]
    public class PlayerEntityPresenter : MonoBehaviour, IEntityPresenter
    {
        [Header("Entity Settings")]
        [SerializeField] int maxHp = 100;
        [SerializeField] float moveSpeed = 5f;
        [SerializeField] float chargeThreshold = 1.0f;
        [SerializeField] float sneakSpeedMultiplier = 0.5f;
        [SerializeField] float invincibilityDuration = 1.0f;

        [Header("Shooting Settings")]
        [SerializeField] BulletPool normalBulletPool;
        [SerializeField] BulletPool chargeBulletPool;
        [SerializeField] int normalShotDamage = 10;
        [SerializeField] int chargedShotDamage = 30;
        [SerializeField] float shootCooldown = 0.2f;
        
        [Header("component references")]
        [SerializeField] PlayerEntityView view;
        [SerializeField] SpriteRenderer spriteRenderer;
        void Reset()
        {
            view = GetComponent<PlayerEntityView>();
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        PlayerEntityModel model;
        Camera mainCamera;
        float lastShootTime;
        Vector2 currentMoveInput;
        readonly CompositeDisposable disposables = new();
        CompositeDisposable inputDisposables;

        public PlayerEntityModel Model => model;
        public IObservable<Unit> OnDeath => model?.OnDeath;

        void Awake()
        {
            if (normalBulletPool == null) Debug.LogError("[PlayerEntityPresenter] NormalBulletPool is not assigned!");
            if (chargeBulletPool == null) Debug.LogError("[PlayerEntityPresenter] ChargeBulletPool is not assigned!");
            mainCamera = Camera.main;
            model = new PlayerEntityModel(maxHp, chargeThreshold, sneakSpeedMultiplier, invincibilityDuration);
        }

        void Start()
        {
            BindModelToView();
            SubscribeToMoveInput();
            SubscribeToAttackInput();
        }

        void BindModelToView()
        {
            model.OnDeath
                .Subscribe(_ => HandleDeath())
                .AddTo(disposables);
        }

        void SubscribeToMoveInput()
        {
            // 移動入力を保持（Updateで使用）
            MessageBroker.Default.Receive<PlayerMoveMessage>()
                .Subscribe(msg =>
                {
                    currentMoveInput = msg.value;
                })
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
                .Where(_ => !model.IsSneaking.Value)
                .Where(_ => Time.time >= lastShootTime + shootCooldown)
                .Subscribe(_ => FireNormalShot())
                .AddTo(inputDisposables);

            // スニークボタンの押下/解除
            MessageBroker.Default.Receive<PlayerCrouchMessage>()
                .Subscribe(msg =>
                {
                    Debug.Log($"[PlayerEntityPresenter] Crouch input: {msg.isPressed}");
                    if (msg.isPressed)
                    {
                        model.SetSneaking(true);
                    }
                    else
                    {
                        if (model.IsChargeComplete)
                        {
                            FireChargedShot();
                            model.ResetCharge();
                        }
                        model.SetSneaking(false);
                    }
                })
                .AddTo(inputDisposables);
        }

        public void UnsubscribeFromAttackInput()
        {
            inputDisposables?.Dispose();
            inputDisposables = new CompositeDisposable();
        }

        void Update()
        {
            if (!model.IsAlive) return;

            // 移動処理（押している間継続）
            Vector3 movement = HandleMovement();
            Vector3 newPosition = ClampToScreen(view.GetPosition() + movement);
            view.UpdatePosition(newPosition);

            // チャージ処理
            if (model.IsSneaking.Value)
            {
                model.UpdateCharge(Time.deltaTime);
            }
        }

        Vector3 HandleMovement()
        {
            if (currentMoveInput.sqrMagnitude < 0.01f) return Vector3.zero;

            float currentSpeed = model.IsSneaking.Value ? moveSpeed * model.SneakSpeedMultiplier : moveSpeed;
            Vector3 movement = new Vector3(currentMoveInput.x, currentMoveInput.y, 0) * currentSpeed * Time.deltaTime;
            return movement;
        }

        Vector3 ClampToScreen(Vector3 position)
        {
            Vector3 extents = spriteRenderer.bounds.extents;

            // Spriteの端がビューポート(0,0)〜(1,1)に収まるようにクランプ
            Vector3 minWorld = mainCamera.ViewportToWorldPoint(Vector3.zero);
            Vector3 maxWorld = mainCamera.ViewportToWorldPoint(Vector3.one);

            position.x = Mathf.Clamp(position.x, minWorld.x + extents.x, maxWorld.x - extents.x);
            position.y = Mathf.Clamp(position.y, minWorld.y + extents.y - 0.2f, maxWorld.y - extents.y - 2.1f);

            return position;
        }

        void FireNormalShot()
        {
            normalBulletPool.SpawnBullet(normalShotDamage, transform.position);
            lastShootTime = Time.time;
        }

        void FireChargedShot()
        {
            chargeBulletPool.SpawnBullet(chargedShotDamage, transform.position + Vector3.right * 2f);
            Debug.Log("[PlayerEntityPresenter] Charged shot fired!");
        }

        void HandleDeath()
        {
            Debug.Log("[PlayerEntityPresenter] Player died");
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            var otherPresenter = other.GetComponent<IEntityPresenter>();
            Debug.Log($"[PlayerEntityPresenter] Collision with {otherPresenter?.GetModel()?.GetType().Name}");
            if (otherPresenter != null)
            {
                model.OnCollision(otherPresenter.GetModel());
                Debug.Log($"[PlayerEntityPresenter] Hp: {model.CurrentHp.Value}");
            }
        }

        void OnDestroy()
        {
            disposables.Dispose();
            model?.Dispose();
        }

        public EntityBase GetModel() => model;
    }
}
