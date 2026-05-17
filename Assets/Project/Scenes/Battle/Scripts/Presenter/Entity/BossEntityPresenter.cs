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
using Project.Scripts.Extensions;
using Project.Scripts.Presenter;

namespace Project.Scenes.Battle.Scripts.Presenter.Entity
{
    [RequireComponent(typeof(BossEntityView))]
    public class BossEntityPresenter : MonoPresenter, IEntityPresenter
    {
        [Header("Entity Settings")]
        [SerializeField] int maxHp = 200;
        [SerializeField] int contactDamage = 20;
        [SerializeField, Range(0f, 1f)] float strongHpRatio = 0.5f;
        [SerializeField, Range(0f, 1f)] float overflowDamageMultiplier = 0f;

        [Header("Attack")]
        [SerializeField] BulletPool[] bulletPools;
        [SerializeField] int bulletDamage = 10;
        [SerializeField] GameObject[] enemySpawnPrefabs;

        [Header("Damage Flash")]
        [SerializeField] float damageFlashInterval = 0.05f;
        [SerializeField] float damageFlashDuration = 0.2f;

        [Header("Component References")]
        [SerializeField] BossEntityView view;
        EnemyTracker enemyTracker;

        void Reset()
        {
            view = GetComponent<BossEntityView>();
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

        BossEntityModel model;
        PlayerEntityPresenter playerPresenter;
        readonly List<IMovementStep> activeMovementSteps = new();
        Tween entranceTween;
        CancellationTokenSource movementCts;
        readonly CompositeDisposable disposables = new();
        IDisposable damageFlashSubscription;

        public BossEntityModel Model => model;
        public IObservable<Unit> OnDeath => model?.OnDeath;

        void Awake()
        {
            if (bulletPools == null || bulletPools.Length == 0) Debug.LogError("[BossEntityPresenter] BulletPools is not assigned!");
            playerPresenter = FindFirstObjectByType<PlayerEntityPresenter>();
            enemyTracker = FindFirstObjectByType<EnemyTracker>();

            model = new BossEntityModel(maxHp, contactDamage, strongHpRatio, overflowDamageMultiplier);

            model.OnDeath
                .Subscribe(_ => HandleDeath())
                .AddTo(disposables);

            SubscribeToDamageFlash();
            SubscribeToHpBar();

            view.UpdatePosition(transform.position);
        }

        void SubscribeToHpBar()
        {
            float normalDenom = model.NormalMaxHp > 0 ? model.NormalMaxHp : 1f;
            float strongDenom = model.StrongMaxHp > 0 ? model.StrongMaxHp : 1f;

            model.NormalHp
                .Subscribe(hp => view.SetNormalHpRatio(hp / normalDenom))
                .AddTo(disposables);

            model.StrongHp
                .Subscribe(hp => view.SetStrongHpRatio(hp / strongDenom))
                .AddTo(disposables);
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
                .Subscribe(ev => HandleAttackEvent(ev))
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

                activeMovementSteps.Add(step);
                var tween = step.Play(transform, Vector2.zero, animator);
                if (tween == null)
                {
                    activeMovementSteps.Remove(step);
                    continue;
                }

                try
                {
                    await tween.ToUniTask(TweenCancelBehaviour.KillAndCancelAwait, ct);
                }
                finally
                {
                    activeMovementSteps.Remove(step);
                }
            }
        }

        void StopMovement()
        {
            if (entranceTween != null && entranceTween.IsActive())
            {
                entranceTween.Complete();
            }
            entranceTween = null;

            // ScriptableObject由来でTween.OnKill経由のキャンセルが効かないケースに備え、
            // movementCts.Cancel()より先に、裏で非同期ループを抱える可能性があるLoopMovementのStepに
            // 明示的な停止を依頼する。先にCancelすると、RunMovementStepsAsyncのfinallyで
            // activeMovementSteps.Remove()が走ってしまい、ここでForceStopできなくなる。
            //
            // ListのスナップショットをとってからForceStopする。ForceStop経由でawaitが解決し
            // RunMovementStepsAsyncのfinallyが同期実行されてリストが変動する可能性があるため。
            var stepsSnapshot = activeMovementSteps.ToArray();
            for (int i = 0; i < stepsSnapshot.Length; i++)
            {
                if (stepsSnapshot[i] is LoopMovementConfig loop)
                {
                    loop.ForceStop();
                }
            }

            movementCts?.Cancel();
            movementCts?.Dispose();
            movementCts = null;

            activeMovementSteps.Clear();
        }

        void Update()
        {
            if (model == null || !model.IsAlive) return;

            model.UpdateAttack(Time.deltaTime);
            view.UpdatePosition(transform.position);
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

        static Quaternion GetRotationAt(AttackEvent ev, int index)
        {
            if (ev.Rotations == null || ev.Rotations.Count == 0) return Quaternion.identity;
            return index < ev.Rotations.Count ? ev.Rotations[index] : ev.Rotations[ev.Rotations.Count - 1];
        }

        BulletPool GetBulletPool(int index)
        {
            if (bulletPools == null || bulletPools.Length == 0) return null;
            if (index < 0 || index >= bulletPools.Length) return bulletPools[0];
            return bulletPools[index];
        }

        GameObject GetEnemySpawnPrefab(int index)
        {
            if (enemySpawnPrefabs == null || enemySpawnPrefabs.Length == 0) return null;
            if (index < 0 || index >= enemySpawnPrefabs.Length) return enemySpawnPrefabs[0];
            return enemySpawnPrefabs[index];
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
            damageFlashSubscription?.Dispose();
            disposables.Dispose();
            model?.AttackStrategy?.Dispose();
            model?.Dispose();
        }

        public EntityBase GetModel() => model;
    }
}
