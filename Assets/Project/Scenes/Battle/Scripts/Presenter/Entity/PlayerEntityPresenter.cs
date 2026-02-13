using System;
using UniRx;
using UnityEngine;
using Project.Scenes.Battle.Scripts.Model.Entity;
using Project.Scenes.Battle.Scripts.View.Entity;
using Project.Scripts.Extensions.Message;

namespace Project.Scenes.Battle.Scripts.Presenter.Entity
{
    [RequireComponent(typeof(PlayerEntityView))]
    public class PlayerEntityPresenter : MonoBehaviour, IEntityPresenter
    {
        [Header("Entity Settings")]
        [SerializeField] int maxHp = 100;
        [SerializeField] float moveSpeed = 5f;
        [SerializeField] float chargeThreshold = 1.0f;
        [SerializeField] float sneakSpeedMultiplier = 0.5f;
        [SerializeField] float invincibilityDuration = 1.0f;

        [Header("Shooting Settings")]
        [SerializeField] BulletPool bulletPool;
        [SerializeField] int normalShotDamage = 10;
        [SerializeField] int chargedShotDamage = 30;
        [SerializeField] float bulletSpeed = 10f;
        [SerializeField] float shootCooldown = 0.2f;

        PlayerEntityView view;
        PlayerEntityModel model;
        float lastShootTime;
        Vector2 currentMoveInput;
        readonly CompositeDisposable disposables = new();

        public PlayerEntityModel Model => model;
        public IObservable<Unit> OnDeath => model?.OnDeath;

        void Awake()
        {
            view = GetComponent<PlayerEntityView>();
            model = new PlayerEntityModel(maxHp, transform.position, chargeThreshold, sneakSpeedMultiplier, invincibilityDuration);
        }

        void Start()
        {
            BindModelToView();
            SubscribeToInput();
        }

        void BindModelToView()
        {
            model.OnDeath
                .Subscribe(_ => HandleDeath())
                .AddTo(disposables);
        }

        void SubscribeToInput()
        {
            // 移動入力を保持（Updateで使用）
            MessageBroker.Default.Receive<PlayerMoveMessage>()
                .Subscribe(msg =>
                {
                    Debug.Log($"[PlayerEntityPresenter] Move input: {msg.value}");
                    currentMoveInput = msg.value;
                })
                .AddTo(disposables);

            // 攻撃ボタンをイベントで処理
            MessageBroker.Default.Receive<PlayerAttackMessage>()
                .Where(_ => !model.IsSneaking.Value)
                .Where(_ => Time.time >= lastShootTime + shootCooldown)
                .Subscribe(_ => FireNormalShot())
                .AddTo(disposables);

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
                .AddTo(disposables);
        }

        void Update()
        {
            if (!model.IsAlive) return;

            // 移動処理（押している間継続）
            HandleMovement();

            // チャージ処理
            if (model.IsSneaking.Value)
            {
                model.UpdateCharge(Time.deltaTime);
            }

            // view.UpdatePosition(transform.position);
        }

        void HandleMovement()
        {
            if (currentMoveInput.sqrMagnitude < 0.01f) return;

            float currentSpeed = model.IsSneaking.Value ? moveSpeed * model.SneakSpeedMultiplier : moveSpeed;
            Vector3 movement = new Vector3(currentMoveInput.x, currentMoveInput.y, 0) * currentSpeed * Time.deltaTime;
            transform.position += movement;
        }

        void FireNormalShot()
        {
            if (bulletPool == null)
            {
                Debug.LogWarning("[PlayerEntityPresenter] BulletPool is not assigned!");
                return;
            }

            Vector3 direction = Vector3.up;
            bulletPool.SpawnBullet(normalShotDamage, transform.position, direction * bulletSpeed, isFriendly: true);
            lastShootTime = Time.time;
        }

        void FireChargedShot()
        {
            if (bulletPool == null)
            {
                Debug.LogWarning("[PlayerEntityPresenter] BulletPool is not assigned!");
                return;
            }

            Vector3 direction = Vector3.up;
            bulletPool.SpawnBullet(chargedShotDamage, transform.position, direction * bulletSpeed, isFriendly: true);
            Debug.Log("[PlayerEntityPresenter] Charged shot fired!");
        }

        void HandleDeath()
        {
            Debug.Log("[PlayerEntityPresenter] Player died");
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
            disposables.Dispose();
            model?.Dispose();
        }

        public EntityBase GetModel() => model;
    }
}
