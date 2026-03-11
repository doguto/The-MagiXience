using System;
using Cysharp.Threading.Tasks.Triggers;
using UniRx;
using UnityEngine;
using Project.Scenes.Battle.Scripts.Model.Entity;
using Project.Scenes.Battle.Scripts.View.Entity;
using Project.Scenes.Battle.Scripts.Model.Movement;
using Project.Scenes.Battle.Scripts.Model.Attack;

namespace Project.Scenes.Battle.Scripts.Presenter.Entity
{
    [RequireComponent(typeof(EnemyEntityView))]
    [RequireComponent(typeof(SpriteRenderer))]
    public class EnemyEntityPresenter : MonoBehaviour, IEntityPresenter
    {
        [Header("Entity Settings")]
        [SerializeField] int maxHp = 50;
        [SerializeField] int contactDamage = 10;

        [Header("Movement")]
        [SerializeReference, SubclassSelector]
        IMovementConfig movementConfig = new StaticMovementConfig();

        [Header("Attack")]
        [SerializeField] BulletPool bulletPool;
        [SerializeField] int bulletDamage = 10;
        [SerializeReference, SubclassSelector]
        IAttackConfig attackConfig = new IntervalAttackConfig();

        [Header("component references")]
        [SerializeField] EnemyEntityView view;
        [SerializeField] SpriteRenderer spriteRenderer;

        void Reset()
        {
            view = GetComponent<EnemyEntityView>();
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        EnemyEntityModel model;
        Camera mainCamera;
        PlayerEntityPresenter playerPresenter;
        readonly CompositeDisposable disposables = new();
        bool isEnteredScreen = false;

        public EnemyEntityModel Model => model;
        public IObservable<Unit> OnDeath => model?.OnDeath;

        void Awake()
        {
            if (bulletPool == null) Debug.LogError("[EnemyEntityPresenter] BulletPool is not assigned!");
            mainCamera = Camera.main;
            playerPresenter = FindFirstObjectByType<PlayerEntityPresenter>();
            Initialize(transform.position);
        }

        public void Initialize(Vector3 spawnPosition)
        {
            model = new EnemyEntityModel(maxHp, spawnPosition, contactDamage);

            // AnimationMovementConfig を含む SequentialMovementConfig には Animator を注入する
            if (movementConfig is SequentialMovementConfig seqConfig)
            {
                var animator = GetComponent<Animator>();
                if (animator != null) seqConfig.InjectAnimator(animator);
            }

            model.SetMovementStrategy(movementConfig?.CreateStrategy() ?? new StaticMovement());

            // 攻撃戦略を設定
            var attackStrategy = attackConfig?.CreateStrategy(() => playerPresenter.transform.position, () => model.Position);
            model.SetAttackStrategy(attackStrategy);

            // OnAttackTiming イベントで弾発射
            model.AttackStrategy?.OnAttackTiming
                .TakeUntil(model.OnDeath)
                .Subscribe(ev => FireBullet(ev))
                .AddTo(disposables);

            BindModelToView();
        }

        void BindModelToView()
        {
            model.OnDeath
                .Subscribe(_ => HandleDeath())
                .AddTo(disposables);

            view.UpdatePosition(transform.position);
        }

        void Update()
        {
            if (model == null || !model.IsAlive) return;

            model.UpdateMovement(Time.deltaTime);
            model.UpdateAttack(Time.deltaTime);

            view.UpdatePosition(model.Position);

            if (IsOutOfScreen())
            {
                Destroy(gameObject);
            }
        }

        bool IsOutOfScreen()
        {
            Vector3 position = model.Position;
            Vector3 viewportPoint = mainCamera.WorldToViewportPoint(position);

            Vector3 extents = spriteRenderer.bounds.extents;
            Vector3 viewportExtents = mainCamera.WorldToViewportPoint(position + extents)
                                    - mainCamera.WorldToViewportPoint(position);
            float margin = Mathf.Max(Mathf.Abs(viewportExtents.x), Mathf.Abs(viewportExtents.y)) + 0.1f;

            bool outOfScreen = viewportPoint.x < -margin || viewportPoint.x > 1f + margin ||
                               viewportPoint.y < -margin || viewportPoint.y > 1f + margin;

            if (!outOfScreen) isEnteredScreen = true;

            return isEnteredScreen && outOfScreen;
        }

        void FireBullet(AttackEvent ev)
        {
            foreach (var dir in ev.Directions)
            {
                bulletPool.SpawnBullet(bulletDamage, bulletPool.transform.position, dir);
            }
        }


        void HandleDeath()
        {
            Debug.Log($"[EnemyEntityPresenter] Enemy died at {transform.position}");
            Destroy(gameObject);
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            var otherPresenter = other.GetComponent<IEntityPresenter>();
            Debug.Log($"[EnemyEntityPresenter] Collision with {otherPresenter?.GetModel()?.GetType().Name}");
            if (otherPresenter != null)
            {
                model.OnCollision(otherPresenter.GetModel());
                Debug.Log($"[EnemyEntityPresenter] Hp: {model.CurrentHp.Value}");
            }
        }

        void OnDestroy()
        {
            disposables.Dispose();
            model?.Dispose();
            model?.AttackStrategy?.Dispose();
        }

        public EntityBase GetModel() => model;
    }

}
