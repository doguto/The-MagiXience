using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UniRx;
using UnityEngine;
using Project.Scenes.Battle.Scripts.Model;
using Project.Scenes.Battle.Scripts.Model.Entity;
using Project.Scenes.Battle.Scripts.Model.Attack;
using Project.Scenes.Battle.Scripts.Model.Movement;
using Project.Scenes.Battle.Scripts.View.Entity;
using Project.Scenes.Global.Scripts.Presenter;
using Project.Scripts.Extensions;

namespace Project.Scenes.Battle.Scripts.Presenter.Entity
{
    [RequireComponent(typeof(EnemyEntityView))]
    public class BossEntityPresenter : MonoBehaviour, IEntityPresenter
    {
        [Header("Entity Settings")]
        [SerializeField] int maxHp = 200;
        [SerializeField] int contactDamage = 20;

        [Header("Attack")]
        [SerializeField] BulletPool[] bulletPools;
        [SerializeField] int bulletDamage = 10;

        [Header("Component References")]
        [SerializeField] EnemyEntityView view;

        void Reset()
        {
            view = GetComponent<EnemyEntityView>();
        }

        EnemyEntityModel model;
        PlayerEntityPresenter playerPresenter;
        SoundManagerPresenter soundManager;
        Tween currentTween;
        Tween entranceTween;
        CancellationTokenSource movementCts;
        readonly CompositeDisposable disposables = new();

        public EnemyEntityModel Model => model;
        public IObservable<Unit> OnDeath => model?.OnDeath;

        void Awake()
        {
            if (bulletPools == null || bulletPools.Length == 0) Debug.LogError("[BossEntityPresenter] BulletPools is not assigned!");
            playerPresenter = FindFirstObjectByType<PlayerEntityPresenter>();
            soundManager = FindFirstObjectByType<SoundManagerPresenter>();

            model = new EnemyEntityModel(maxHp, contactDamage);

            model.OnDeath
                .Subscribe(_ => HandleDeath())
                .AddTo(disposables);

            view.UpdatePosition(transform.position);
        }

        public void OnPhaseStarted(BattlePhaseModelBase phase, BattleTimelineBuilderAsset builder)
        {
            if (builder == null)
            {
                ApplyAttackTimeline(null);
                StartMovementSequence(null);
                return;
            }

            var attackPreset = builder.BossAttackPreset;
            var attackTimeline = attackPreset != null
                ? attackPreset.CreateTimeline()
                : null;
            ApplyAttackTimeline(attackTimeline);

            var movementSteps = builder.BossMovementPreset != null
                ? builder.BossMovementPreset.Steps
                : null;
            StartMovementSequence(movementSteps);
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
            attackTimeline.InitializeProviders(getPlayerPos, () => transform.position, () => transform.rotation);
            model.SetAttackStrategy(attackTimeline);

            model.AttackStrategy.OnAttackTiming
                .TakeUntil(model.OnDeath)
                .Subscribe(ev => FireBullet(ev))
                .AddTo(disposables);
        }

        void StartMovementSequence(IReadOnlyList<IMovementStep> steps)
        {
            StopMovement();

            if (steps == null || steps.Count == 0) return;

            movementCts = new CancellationTokenSource();
            var animator = GetComponent<Animator>();
            RunMovementStepsAsync(steps, animator, movementCts.Token).Forget();
        }

        public void PlayEntranceMovement(IReadOnlyList<IMovementStep> steps)
        {
            if (steps == null || steps.Count == 0) return;

            var animator = GetComponent<Animator>();
            var sequence = DOTween.Sequence();
            foreach (var step in steps)
            {
                if (step == null) continue;
                sequence.Append(step.Play(transform, Vector2.zero, animator));
            }
            entranceTween = sequence;
        }

        async UniTaskVoid RunMovementStepsAsync(IReadOnlyList<IMovementStep> steps, Animator animator, CancellationToken ct)
        {
            foreach (var step in steps)
            {
                if (step == null) continue;
                ct.ThrowIfCancellationRequested();
                currentTween = step.Play(transform, Vector2.zero, animator);
                await currentTween.ToUniTask(TweenCancelBehaviour.KillAndCancelAwait, ct);
            }
        }

        void StopMovement()
        {
            if (entranceTween != null && entranceTween.IsActive())
            {
                entranceTween.Complete();
            }
            entranceTween = null;

            movementCts?.Cancel();
            movementCts?.Dispose();
            movementCts = null;
            currentTween?.Kill();
            currentTween = null;
        }

        void Update()
        {
            if (model == null || !model.IsAlive) return;

            model.UpdateAttack(Time.deltaTime);
            view.UpdatePosition(transform.position);
        }

        void FireBullet(AttackEvent ev)
        {
            var pool = GetBulletPool(ev.BulletPoolIndex);
            if (pool == null) return;

            if (ev.SeType != SeType.None)
            {
                soundManager?.PlaySE(ev.SeType);
            }

            foreach (var dir in ev.Directions)
            {
                pool.SpawnBullet(bulletDamage, pool.transform.position, dir, rotation: transform.rotation);
            }
        }

        BulletPool GetBulletPool(int index)
        {
            if (bulletPools == null || bulletPools.Length == 0) return null;
            if (index < 0 || index >= bulletPools.Length) return bulletPools[0];
            return bulletPools[index];
        }

        void HandleDeath()
        {
            StopMovement();
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
            StopMovement();
            disposables.Dispose();
            model?.AttackStrategy?.Dispose();
            model?.Dispose();
        }

        public EntityBase GetModel() => model;
    }
}
