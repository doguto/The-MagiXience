using System;
using System.Collections.Generic;
using DG.Tweening;
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
        List<IMovementStep> movementSteps = new() { new InfiniteMovementConfig() };

        [Header("Attack")]
        [SerializeField] BulletPool bulletPool;
        [SerializeField] int bulletDamage = 10;
        [SerializeField] AttackTimeline attackTimeline;

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
        Sequence movementSequence;
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
            transform.position = spawnPosition;
            model = new EnemyEntityModel(maxHp, contactDamage);

            var animator = GetComponent<Animator>();
            StartMovementSequence(animator);

            if (attackTimeline != null)
            {
                Func<Vector3> getPlayerPos = () => playerPresenter != null ? playerPresenter.transform.position : Vector3.zero;
                attackTimeline.InitializeProviders(getPlayerPos, () => transform.position);
            }
            model.SetAttackStrategy(attackTimeline);

            model.AttackStrategy?.OnAttackTiming
                .TakeUntil(model.OnDeath)
                .Subscribe(ev => FireBullet(ev))
                .AddTo(disposables);

            model.OnDeath
                .Subscribe(_ => HandleDeath())
                .AddTo(disposables);

            view.UpdatePosition(transform.position);
        }

        void StartMovementSequence(Animator animator)
        {
            movementSequence?.Kill();

            if (movementSteps == null || movementSteps.Count == 0) return;

            movementSequence = DOTween.Sequence();
            foreach (var step in movementSteps)
            {
                if (step == null) continue;
                movementSequence.Append(step.Play(transform, Vector2.zero, animator));
            }
        }

        void Update()
        {
            if (model == null || !model.IsAlive) return;

            model.UpdateAttack(Time.deltaTime);

            view.UpdatePosition(transform.position);

            if (IsOutOfScreen())
            {
                Destroy(gameObject);
            }
        }

        bool IsOutOfScreen()
        {
            Vector3 position = transform.position;
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
            movementSequence?.Kill();
            Debug.Log($"[EnemyEntityPresenter] Enemy died at {transform.position}");
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
            movementSequence?.Kill();
            disposables.Dispose();
            model?.Dispose();
            model?.AttackStrategy?.Dispose();
        }

        public EntityBase GetModel() => model;
    }
}
