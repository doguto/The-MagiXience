using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UniRx;
using UnityEngine;
using Project.Scenes.Battle.Scripts.Model;
using Project.Scenes.Battle.Scripts.Model.Entity;
using Project.Scenes.Battle.Scripts.View.Entity;
using Project.Scenes.Battle.Scripts.Model.Movement;
using Project.Scenes.Battle.Scripts.Model.Attack;
using Project.Scripts.Extensions;
using Project.Scripts.Presenter;

namespace Project.Scenes.Battle.Scripts.Presenter.Entity
{
    [RequireComponent(typeof(EnemyEntityView))]
    [RequireComponent(typeof(SpriteRenderer))]
    public class EnemyEntityPresenter : MonoPresenter, IEntityPresenter
    {
        // NOTE: 新規プロパティを追加したらEnemyEntityPresenterEditor.csも編集すること
        [Header("Entity Settings")] [SerializeField]
        int maxHp = 50;

        [SerializeField] int contactDamage = 10;
        [SerializeField] float lifetime = 0f;

        [Header("Movement")] [SerializeField] MovementPreset movementPreset;

        [SerializeReference] [SubclassSelector]
        List<IMovementStep> movementSteps = new() { new InfiniteMovementConfig() };

        [Header("Attack")] [SerializeField] BulletPool[] bulletPools;
        [SerializeField] int bulletDamage = 10;
        [SerializeField] GameObject[] enemySpawnPrefabs;
        [SerializeField] AttackPreset attackPreset;
        [SerializeField] AttackTimeline attackTimeline;

        [Header("Death Attack")]
        [SerializeField] AttackTimelineEntry deathAttackEntry;

        [Header("Damage Flash")]
        [SerializeField] float damageFlashInterval = 0.05f;
        [SerializeField] float damageFlashDuration = 0.2f;

        [Header("component references")] [SerializeField]
        EnemyEntityView view;

        [SerializeField] SpriteRenderer spriteRenderer;
        EnemyTracker enemyTracker;

        void Reset()
        {
            view = GetComponent<EnemyEntityView>();
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            if (Application.isPlaying) return;
            UnityEditor.EditorApplication.delayCall += AppendChildBulletPools;
        }

        void AppendChildBulletPools()
        {
            if (this == null) return;

            var children = GetComponentsInChildren<BulletPool>(true);
            if (children == null || children.Length == 0) return;

            var current = bulletPools ?? Array.Empty<BulletPool>();
            var appended = new List<BulletPool>(current);
            bool changed = false;

            foreach (var child in children)
            {
                if (child == null) continue;
                if (Array.IndexOf(current, child) >= 0) continue;
                appended.Add(child);
                changed = true;
            }

            if (!changed) return;

            bulletPools = appended.ToArray();
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif

        EnemyEntityModel model;
        PlayerEntityPresenter playerPresenter;
        Tween currentTween;
        CancellationTokenSource movementCts;
        CancellationTokenSource lifetimeCts;
        readonly CompositeDisposable disposables = new();
        IDisposable damageFlashSubscription;
        bool isEnteredScreen = false;

        public EnemyEntityModel Model => model;
        public IObservable<Unit> OnDeath => model?.OnDeath;

        void Awake()
        {
            if (bulletPools == null || bulletPools.Length == 0) Debug.LogError("[EnemyEntityPresenter] BulletPools is not assigned!");
            playerPresenter = FindFirstObjectByType<PlayerEntityPresenter>();
            enemyTracker = FindFirstObjectByType<EnemyTracker>();
            Initialize(transform.position);
        }

        public void Initialize(Vector3 spawnPosition)
        {
            transform.position = spawnPosition;
            model = new EnemyEntityModel(maxHp, contactDamage);

            var animator = GetComponent<Animator>();

            // Movement: プリセット優先、なければインライン
            var steps = movementPreset != null
                ? movementPreset.Steps
                : movementSteps;
            StartMovementSequence(steps, animator);

            // Attack: プリセット優先、なければインライン
            var timeline = attackPreset != null
                ? attackPreset.CreateTimeline()
                : attackTimeline;

            Func<Vector3> getPlayerPos = () => playerPresenter != null ? playerPresenter.transform.position : Vector3.zero;

            if (timeline != null)
            {
                timeline.InitializeProviders(getPlayerPos, () => transform.position, () => transform.rotation);
            }

            model.SetAttackStrategy(timeline);

            InitializeDeathAttackEntry(getPlayerPos);

            model.AttackStrategy?.OnAttackTiming
                 .TakeUntil(model.OnDeath)
                 .Subscribe(ev => HandleAttackEvent(ev))
                 .AddTo(disposables);

            model.OnDeath
                 .Subscribe(_ => HandleDeath())
                 .AddTo(disposables);

            SubscribeToDamageFlash();

            view.UpdatePosition(transform.position);

            StartLifetimeCountdown();
        }

        void InitializeDeathAttackEntry(Func<Vector3> getPlayerPos)
        {
            if (deathAttackEntry == null) return;

            deathAttackEntry.directionProvider?.Initialize(getPlayerPos, () => transform.position, () => transform.rotation);
            deathAttackEntry.rotationProvider?.Initialize(getPlayerPos, () => transform.position, () => transform.rotation);
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
            Destroy(gameObject);
        }

        void StartMovementSequence(IReadOnlyList<IMovementStep> steps, Animator animator)
        {
            StopMovement();

            if (steps == null || steps.Count == 0) return;

            movementCts = new CancellationTokenSource();
            RunMovementStepsAsync(steps, animator, movementCts.Token).Forget();
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

            if (IsOutOfScreen())
            {
                Destroy(gameObject);
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

        void HandleAttackEvent(AttackEvent ev)
        {
            switch (ev.Type)
            {
                case AttackEventType.Bullet:
                    FireBullet(ev);
                    break;
                case AttackEventType.EnemySpawn:
                    SpawnEnemy(ev);
                    break;
            }
        }

        void FireBullet(AttackEvent ev)
        {
            if (ev.Directions == null) return;

            var pool = GetBulletPool(ev.SourceIndex);
            if (pool == null) return;

            if (ev.SeType != SeType.None)
            {
                soundManager?.PlaySE(ev.SeType);
            }
            for (int i = 0; i < ev.Directions.Count; i++)
            {
                pool.SpawnBullet(bulletDamage, pool.transform.position, ev.Directions[i], rotation: GetRotationAt(ev, i));
            }
        }

        BulletPool GetBulletPool(int index)
        {
            if (bulletPools == null || bulletPools.Length == 0) return null;
            if (index < 0 || index >= bulletPools.Length) return bulletPools[0];
            return bulletPools[index];
        }

        void SpawnEnemy(AttackEvent ev)
        {
            var prefab = GetEnemySpawnPrefab(ev.SourceIndex);
            if (prefab == null) return;

            if (ev.SeType != SeType.None)
            {
                soundManager?.PlaySE(ev.SeType);
            }

            if (ev.SpawnOffsets == null) return;
            for (int i = 0; i < ev.SpawnOffsets.Count; i++)
            {
                // Instantiateの3引数版ではrotationが反映されないため、生成後にSetPositionAndRotationで明示的に設定する
                var instance = Instantiate(prefab);
                instance.transform.SetPositionAndRotation(transform.position + (Vector3)ev.SpawnOffsets[i], GetRotationAt(ev, i));

                if (enemyTracker != null && instance.TryGetComponent<EnemyEntityPresenter>(out var enemyPresenter))
                {
                    enemyTracker.RegisterEnemy(enemyPresenter);
                }
            }
        }

        Quaternion GetRotationAt(AttackEvent ev, int index)
        {
            if (ev.Rotations == null || ev.Rotations.Count == 0) return Quaternion.identity;
            return index < ev.Rotations.Count ? ev.Rotations[index] : ev.Rotations[ev.Rotations.Count - 1];
        }

        GameObject GetEnemySpawnPrefab(int index)
        {
            if (enemySpawnPrefabs == null || enemySpawnPrefabs.Length == 0) return null;
            if (index < 0 || index >= enemySpawnPrefabs.Length) return enemySpawnPrefabs[0];
            return enemySpawnPrefabs[index];
        }

        void HandleDeath()
        {
            FireDeathAttack();
            StopMovement();
            Destroy(gameObject);
        }

        void FireDeathAttack()
        {
            if (deathAttackEntry?.signal == null) return;

            var sourceIndex = deathAttackEntry.sourceIndexProvider?.Get() ?? 0;
            var ev = deathAttackEntry.signal.CreateEvent(
                deathAttackEntry.directionProvider,
                deathAttackEntry.rotationProvider,
                sourceIndex,
                deathAttackEntry.seType);
            HandleAttackEvent(ev);
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
            lifetimeCts?.Cancel();
            lifetimeCts?.Dispose();
            lifetimeCts = null;
            damageFlashSubscription?.Dispose();
            damageFlashSubscription = null;
            disposables.Dispose();
            model?.Dispose();
            model?.AttackStrategy?.Dispose();
        }

        public EntityBase GetModel()
        {
            return model;
        }
    }
}
