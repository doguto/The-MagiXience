using System;
using UniRx;
using UnityEngine;
using Project.Scenes.Battle.Scripts.Model.Entity;
using Project.Scenes.Battle.Scripts.View.Entity;
using Project.Scenes.Battle.Scripts.Model.Movement;
using Project.Scenes.Battle.Scripts.Model.Attack;

namespace Project.Scenes.Battle.Scripts.Presenter.Entity
{
    [RequireComponent(typeof(EnemyEntityView))]
    public class EnemyEntityPresenter : MonoBehaviour, IEntityPresenter
    {
        [SerializeField] int maxHp = 50;
        [SerializeField] int contactDamage = 10;
        [SerializeField] BulletPool bulletPool;
        [SerializeField] int bulletDamage = 10;
        [SerializeField] float bulletSpeed = 5f;
        [SerializeField] float attackInterval = 2.0f;

        EnemyEntityView view;
        EnemyEntityModel model;
        readonly CompositeDisposable disposables = new();

        public EnemyEntityModel Model => model;
        public IObservable<Unit> OnDeath => model?.OnDeath;

        void Awake()
        {
            view = GetComponent<EnemyEntityView>();
            // 仮のInitialize
            // 本来は外部からやるのがいいと思う
            Initialize(transform.position, maxHp, contactDamage, new StaticMovement());
        }

        public void Initialize(Vector3 spawnPosition, int hp, int damage, IMovementStrategy movementStrategy)
        {
            maxHp = hp;
            contactDamage = damage;
            model = new EnemyEntityModel(maxHp, spawnPosition, contactDamage);

            model.SetMovementStrategy(movementStrategy);

            // 攻撃戦略を設定
            var attackStrategy = new IntervalAttackStrategy(attackInterval);
            model.SetAttackStrategy(attackStrategy);

            // OnAttackTiming イベントで弾発射
            model.AttackStrategy.OnAttackTiming
                .TakeUntil(model.OnDeath)
                .Subscribe(_ => FireBullet())
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
        }

        void FireBullet()
        {
            if (bulletPool == null)
            {
                Debug.LogWarning("[EnemyEntityPresenter] BulletPool is not assigned!");
                return;
            }

            Vector3 direction = Vector3.left;
            bulletPool.SpawnBullet(bulletDamage, model.Position, direction * bulletSpeed, isFriendly: false);
            Debug.Log("[EnemyEntityPresenter] Enemy fired bullet!");
        }

        void HandleDeath()
        {
            Debug.Log($"[EnemyEntityPresenter] Enemy died at {transform.position}");

            Observable.Timer(TimeSpan.FromSeconds(1f))
                .Subscribe(_ => Destroy(gameObject))
                .AddTo(disposables);
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
