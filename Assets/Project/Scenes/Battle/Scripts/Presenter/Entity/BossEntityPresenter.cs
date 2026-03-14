using System;
using System.Collections.Generic;
using DG.Tweening;
using UniRx;
using UnityEngine;
using Project.Scenes.Battle.Scripts.Model;
using Project.Scenes.Battle.Scripts.Model.Entity;
using Project.Scenes.Battle.Scripts.Model.Attack;
using Project.Scenes.Battle.Scripts.Model.Movement;
using Project.Scenes.Battle.Scripts.View.Entity;

namespace Project.Scenes.Battle.Scripts.Presenter.Entity
{
    [RequireComponent(typeof(EnemyEntityView))]
    public class BossEntityPresenter : MonoBehaviour, IEntityPresenter
    {
        [Header("Entity Settings")]
        [SerializeField] int maxHp = 200;
        [SerializeField] int contactDamage = 20;

        [Header("Attack")]
        [SerializeField] BulletPool bulletPool;
        [SerializeField] int bulletDamage = 10;

        [Header("Component References")]
        [SerializeField] EnemyEntityView view;

        void Reset()
        {
            view = GetComponent<EnemyEntityView>();
        }

        EnemyEntityModel model;
        PlayerEntityPresenter playerPresenter;
        Sequence movementSequence;
        readonly CompositeDisposable disposables = new();

        public EnemyEntityModel Model => model;
        public IObservable<Unit> OnDeath => model?.OnDeath;

        void Awake()
        {
            if (bulletPool == null) Debug.LogError("[BossEntityPresenter] BulletPool is not assigned!");
            playerPresenter = FindFirstObjectByType<PlayerEntityPresenter>();

            model = new EnemyEntityModel(maxHp, contactDamage);

            model.OnDeath
                .Subscribe(_ => HandleDeath())
                .AddTo(disposables);

            view.UpdatePosition(transform.position);
        }

        public void OnPhaseStarted(BattlePhaseModelBase phase)
        {
            var builder = phase.Builder;
            if (builder == null) return;

            ApplyAttackTimeline(builder.BossAttackTimeline);
            StartMovementSequence(builder.BossMovementSteps);
        }

        void ApplyAttackTimeline(AttackTimeline attackTimeline)
        {
            model.AttackStrategy?.Dispose();

            if (attackTimeline == null)
            {
                model.SetAttackStrategy(null);
                return;
            }

            Func<Vector3> getPlayerPos = () => playerPresenter != null ? playerPresenter.transform.position : Vector3.zero;
            attackTimeline.InitializeProviders(getPlayerPos, () => transform.position);
            model.SetAttackStrategy(attackTimeline);

            model.AttackStrategy.OnAttackTiming
                .TakeUntil(model.OnDeath)
                .Subscribe(ev => FireBullet(ev))
                .AddTo(disposables);
        }

        void StartMovementSequence(IReadOnlyList<IMovementStep> steps)
        {
            movementSequence?.Kill();

            if (steps == null || steps.Count == 0) return;

            var animator = GetComponent<Animator>();
            movementSequence = DOTween.Sequence();
            foreach (var step in steps)
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
            Debug.Log("[BossEntityPresenter] Boss died.");
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
            model?.AttackStrategy?.Dispose();
            model?.Dispose();
        }

        public EntityBase GetModel() => model;
    }
}
