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
        bool isAttackButtonPressed;
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
            model.CurrentHp
                .Subscribe(hp => view.UpdateHpDisplay(hp, model.MaxHp))
                .AddTo(disposables);

            model.OnDeath
                .Subscribe(_ => HandleDeath())
                .AddTo(disposables);

            model.IsSneaking
                .Subscribe(isSneaking => view.SetSneakVisual(isSneaking))
                .AddTo(disposables);

            model.ChargeTime
                .Subscribe(chargeTime =>
                {
                    float ratio = chargeTime / model.ChargeThreshold;
                    view.UpdateChargeVisual(ratio);
                })
                .AddTo(disposables);

            model.IsInvincible
                .Subscribe(isInvincible => view.SetInvincibilityVisual(isInvincible))
                .AddTo(disposables);
        }

        void SubscribeToInput()
        {
            // 移動入力を購読
            MessageBroker.Default.Receive<PlayerMoveMessage>()
                .Subscribe(msg => currentMoveInput = msg.value)
                .AddTo(disposables);

            // 攻撃ボタンの購読
            MessageBroker.Default.Receive<PlayerAttackMessage>()
                .Subscribe(_ => isAttackButtonPressed = true)
                .AddTo(disposables);

            // スニークボタン（Crouchを使用）の押下/解除
            MessageBroker.Default.Receive<PlayerCrouchMessage>()
                .Subscribe(msg =>
                {
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

            HandleMovement();

            if (model.IsSneaking.Value)
            {
                model.UpdateCharge(Time.deltaTime);
            }

            model.UpdateInvincibility(Time.deltaTime);

            HandleShooting();

            view.UpdatePosition(transform.position);
        }

        void HandleMovement()
        {
            if (currentMoveInput.sqrMagnitude < 0.01f) return;

            float currentSpeed = model.IsSneaking.Value ? moveSpeed * model.SneakSpeedMultiplier : moveSpeed;
            Vector3 movement = new Vector3(currentMoveInput.x, currentMoveInput.y, 0) * currentSpeed * Time.deltaTime;
            transform.position += movement;
        }

        void HandleShooting()
        {
            if (model.IsSneaking.Value) return;

            if (Time.time < lastShootTime + shootCooldown) return;

            if (isAttackButtonPressed)
            {
                FireNormalShot();
                lastShootTime = Time.time;
                isAttackButtonPressed = false;
            }
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
            view.PlayShootEffect();
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
            view.PlayShootEffect();
            Debug.Log("[PlayerEntityPresenter] Charged shot fired!");
        }

        void HandleDeath()
        {
            view.PlayDeathEffect();
            Debug.Log("[PlayerEntityPresenter] Player died");
        }

        public void TakeDamage(int damage)
        {
            model.TakeDamage(damage);
            view.PlayDamageEffect();
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
