using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UniRx;
using UnityEngine;
using Project.Scenes.Battle.Scripts.Model;
using Project.Scenes.Battle.Scripts.Model.Entity;
using Project.Scenes.Battle.Scripts.View;
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
        [Tooltip("ONにすると通常攻撃は無効（被ダメージ0）、チャージ攻撃のみ有効")]
        [SerializeField] bool onlyChargeDamageable = false;
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
        IDisposable attackTimingSubscription;
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
            model = new EnemyEntityModel(maxHp, contactDamage, onlyChargeDamageable);

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
                timeline.InitializeProviders(getPlayerPos, () => transform.position, () => transform.rotation, model.CurrentHp, model.MaxHp);
            }

            model.SetAttackStrategy(timeline);

            InitializeDeathAttackEntry(getPlayerPos);

            SubscribeAttackTiming();

            model.OnDeath
                 .Subscribe(_ => HandleDeath())
                 .AddTo(disposables);

            model.OnIneffectiveHit
                 .TakeUntil(model.OnDeath)
                 .Subscribe(_ => soundManager?.PlaySE(SeType.Metal))
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

        /// <summary>
        /// Initialize後に外部から動きを差し替える。Timeline経由/Attack経由のスポーンで使用。
        /// StartMovementSequenceが内部でStopMovementするため、Initialize直後に呼んでも安全。
        /// </summary>
        public void OverrideMovement(MovementPreset preset)
        {
            if (preset == null) return;
            StartMovementSequence(preset.Steps, GetComponent<Animator>());
        }

        /// <summary>
        /// Initialize後に外部から攻撃パターンを差し替える。Timeline経由のスポーンで使用。
        /// AttackTimelineは内部にSubject/タイマー購読を持つため、差し替え前に旧StrategyをDisposeして後始末する。
        /// </summary>
        public void OverrideAttack(AttackPreset preset)
        {
            if (preset == null) return;

            model.AttackStrategy?.Dispose();

            var timeline = preset.CreateTimeline();
            Func<Vector3> getPlayerPos = () => playerPresenter != null ? playerPresenter.transform.position : Vector3.zero;
            timeline?.InitializeProviders(getPlayerPos, () => transform.position, () => transform.rotation, model.CurrentHp, model.MaxHp);

            model.SetAttackStrategy(timeline);
            SubscribeAttackTiming();
        }

        void SubscribeAttackTiming()
        {
            attackTimingSubscription?.Dispose();
            attackTimingSubscription = model.AttackStrategy?.OnAttackTiming
                .TakeUntil(model.OnDeath)
                .Subscribe(ev => HandleAttackEvent(ev));
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
                case AttackEventType.SetInvincible:
                    model.SetInvincible(ev.Enabled);
                    break;
                case AttackEventType.Despawn:
                    StopMovement();
                    Destroy(gameObject);
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
                var spawnDelay = ev.GetSpawnDelayAt(i);
                if (spawnDelay > 0f)
                {
                    SpawnBulletDelayed(pool, ev, i, spawnDelay).Forget();
                }
                else
                {
                    pool.SpawnBullet(bulletDamage, GetSpawnPosition(ev, pool.transform.position, i), ev.Directions[i], rotation: GetRotationAt(ev, i), range: ev.Range, startDelay: ev.GetStartDelayAt(i));
                }
            }
        }

        async UniTaskVoid SpawnBulletDelayed(BulletPool pool, AttackEvent ev, int index, float spawnDelay)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(spawnDelay), cancellationToken: this.GetCancellationTokenOnDestroy());
            if (pool == null) return;

            pool.SpawnBullet(bulletDamage, GetSpawnPosition(ev, pool.transform.position, index), ev.Directions[index], rotation: GetRotationAt(ev, index), range: ev.Range, startDelay: ev.GetStartDelayAt(index));
        }

        Vector3 GetSpawnPosition(AttackEvent ev, Vector3 basePosition, int index)
        {
            if (ev.SpawnOffsets == null || index >= ev.SpawnOffsets.Count) return basePosition;

            var offset = ev.SpawnOffsets[index];
            // World指定のとき SpawnOffsets は発射元からの相対ではなくワールド座標そのもの。
            // zは発射元のものを引き継いで、描画順が変わらないようにする。
            if (ev.SpawnSpace == AttackSpawnSpace.World) return new Vector3(offset.x, offset.y, basePosition.z);

            return basePosition + (Vector3)offset;
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
                instance.transform.SetPositionAndRotation(GetSpawnPosition(ev, transform.position, i), GetRotationAt(ev, i));

                // 予告線のように「線分の長さ」を生成後に教える必要があるViewへ、Startが走る前に流し込む
                if (instance.TryGetComponent<IBeamVisualReceiver>(out var beamVisual))
                {
                    beamVisual.ConfigureBeam(ev.Range, ev.Duration, ev.Width);
                }

                if (instance.TryGetComponent<EnemyEntityPresenter>(out var enemyPresenter))
                {
                    if (ev.MovementOverride != null) enemyPresenter.OverrideMovement(ev.MovementOverride);
                    enemyTracker?.RegisterEnemy(enemyPresenter);
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
            attackTimingSubscription?.Dispose();
            attackTimingSubscription = null;
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
