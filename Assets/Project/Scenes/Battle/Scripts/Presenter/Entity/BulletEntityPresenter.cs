using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UniRx;
using UnityEngine;
using UnityEngine.Pool;
using Project.Scenes.Battle.Scripts.Model;
using Project.Scenes.Battle.Scripts.Model.Entity;
using Project.Scenes.Battle.Scripts.Model.Movement;
using Project.Scenes.Battle.Scripts.View.Entity;

namespace Project.Scenes.Battle.Scripts.Presenter.Entity
{
    [RequireComponent(typeof(BulletEntityView))]
    [RequireComponent(typeof(SpriteRenderer))]
    public class BulletEntityPresenter : MonoBehaviour, IEntityPresenter
    {
        [SerializeReference, SubclassSelector]
        List<IMovementStep> movementSteps = new() { new InfiniteMovementConfig() };

        [SerializeField] BulletEntityView view;
        [SerializeField] SpriteRenderer spriteRenderer;
        [SerializeField, Tooltip("弾のライフタイム（秒）。0以下で無制限")]
        float lifetime = 5f;

        void Reset()
        {
            view = GetComponent<BulletEntityView>();
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        BulletEntityModel model;
        IObjectPool<BulletEntityPresenter> pool;
        Vector3 travelOrigin;
        Vector2 travelAxis;
        float travelRange;
        Tween currentTween;
        CancellationTokenSource movementCts;
        CancellationTokenSource lifetimeCts;
        readonly CompositeDisposable disposables = new();

        public BulletEntityModel Model => model;

        /// <param name="range">飛距離の上限(ワールド単位)。0以下で無制限</param>
        /// <param name="startDelay">生成後、移動を開始するまでの待機秒数。0で従来通り即座に移動開始</param>
        /// <param name="movementOverride">この弾に限り使う移動ステップ。nullならPrefab側のmovementStepsを使う(従来動作)</param>
        public void Initialize(int damage, Vector3 position, Vector2 direction, IObjectPool<BulletEntityPresenter> objectPool, bool isPlayerBullet = false, Quaternion rotation = default, float range = 0f, float startDelay = 0f, IMovementStep movementOverride = null)
        {
            pool = objectPool;
            var resolvedRotation = rotation == default ? Quaternion.identity : rotation;
            transform.SetPositionAndRotation(position, resolvedRotation);

            travelOrigin = position;
            travelAxis = direction.normalized;
            travelRange = travelAxis == Vector2.zero ? 0f : range;

            if (model == null)
                model = new BulletEntityModel(damage, isPlayerBullet);
            else
                model.Reinitialize(damage, isPlayerBullet);

            StartMovementSequence(direction, startDelay, movementOverride);

            view.ResetView();
            view.UpdatePosition(position);

            BindModelToView();
        }

        void StartMovementSequence(Vector2 direction, float startDelay, IMovementStep movementOverride)
        {
            StopMovement();

            bool isOverride = movementOverride != null;
            IReadOnlyList<IMovementStep> steps = isOverride ? new[] { movementOverride } : movementSteps;
            if (steps == null || steps.Count == 0) return;

            movementCts = new CancellationTokenSource();
            RunMovementStepsAsync(steps, direction, startDelay, isOverride, movementCts.Token).Forget();
        }

        // movementOverride経由(AttackEvent.BulletMovement)の一時的な移動は、完了と同時に役目を終えるため
        // Prefab固定のlifetimeを待たずに自分でプールへ返す。通常のmovementSteps(Prefab固定)はそのまま留まる。
        async UniTaskVoid RunMovementStepsAsync(IReadOnlyList<IMovementStep> steps, Vector2 direction, float startDelay, bool returnToPoolWhenDone, CancellationToken ct)
        {
            if (startDelay > 0f)
                await UniTask.Delay(TimeSpan.FromSeconds(startDelay), cancellationToken: ct);

            foreach (var step in steps)
            {
                if (step == null) continue;
                ct.ThrowIfCancellationRequested();

                bool grantsInvincibility = step is IInvincibilityGrantingStep;
                if (grantsInvincibility) model.SetInvincible(true);

                try
                {
                    currentTween = step.Play(transform, direction, null);
                    await currentTween.ToUniTask(TweenCancelBehaviour.KillAndCancelAwait, ct);
                }
                finally
                {
                    if (grantsInvincibility) model.SetInvincible(false);
                }
            }

            if (returnToPoolWhenDone) ReturnToPool();
        }

        void StopMovement()
        {
            movementCts?.Cancel();
            movementCts?.Dispose();
            movementCts = null;
            currentTween = null;
        }

        void BindModelToView()
        {
            disposables.Clear();

            model.CurrentHp
                .Where(hp => hp <= 0)
                .Subscribe(_ => HandleDestruction())
                .AddTo(disposables);

            StartLifetimeTimer();
        }

        void Update()
        {
            if (model == null || !model.IsAlive) return;

            view.UpdatePosition(transform.position);

            if (IsOutOfScreen() || HasReachedRangeLimit())
            {
                ReturnToPool();
            }
        }

        bool HasReachedRangeLimit()
        {
            if (travelRange <= 0f) return false;

            // ジグザグ移動は軸の左右に蛇行するため、起点からの直線距離だと終点より手前で消えてしまう。
            // ビーム軸へ射影した「軸方向の進捗」で判定することで、終点をきっちり揃える。
            var traveled = Vector2.Dot((Vector2)(transform.position - travelOrigin), travelAxis);
            return traveled >= travelRange;
        }

        bool IsOutOfScreen()
        {
            var position = transform.position;
            var extents = spriteRenderer.bounds.extents;
            var margin = Mathf.Max(extents.x, extents.y) + 0.1f;

            return position.x < ScreenBoundsCache.MinX - margin || position.x > ScreenBoundsCache.MaxX + margin ||
                   position.y < ScreenBoundsCache.MinY - margin || position.y > ScreenBoundsCache.MaxY + margin;
        }

        void StartLifetimeTimer()
        {
            CancelLifetimeTimer();
            if (lifetime <= 0f) return;

            lifetimeCts = new CancellationTokenSource();
            LifetimeTimerAsync(lifetimeCts.Token).Forget();
        }

        async UniTaskVoid LifetimeTimerAsync(CancellationToken ct)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(lifetime), cancellationToken: ct);
            ReturnToPool();
        }

        void CancelLifetimeTimer()
        {
            lifetimeCts?.Cancel();
            lifetimeCts?.Dispose();
            lifetimeCts = null;
        }

        void HandleDestruction() => ReturnToPool();

        public void ReturnToPool()
        {
            StopMovement();
            if (pool != null)
                pool.Release(this);
            else
                Destroy(gameObject);
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (model == null || !model.IsAlive) return;
            var otherPresenter = other.GetComponent<IEntityPresenter>();
            if (otherPresenter != null)
                model.OnCollision(otherPresenter.GetModel());
        }

        public void OnReturnedToPool()
        {
            CancelLifetimeTimer();
            StopMovement();
            view.SetVisible(false);
            gameObject.SetActive(false);
        }

        public void OnTakenFromPool()
        {
            gameObject.SetActive(true);
        }

        void OnDestroy()
        {
            CancelLifetimeTimer();
            StopMovement();
            disposables.Dispose();
            model?.Dispose();
        }

        public EntityBase GetModel() => model;
    }
}
