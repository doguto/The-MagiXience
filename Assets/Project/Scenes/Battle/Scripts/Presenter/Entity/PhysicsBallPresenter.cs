using System;
using UniRx;
using UnityEngine;
using Project.Scenes.Battle.Scripts.Model.Entity;

namespace Project.Scenes.Battle.Scripts.Presenter.Entity
{
    /// <summary>
    /// 重力を持ち、壁やスペクトラムバーで反射する物理ボール。
    /// EnemyとしてEnemySpawnTrackからスポーン可能。
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class PhysicsBallPresenter : MonoBehaviour, IEntityPresenter
    {
        [Header("Entity Settings")]
        [SerializeField] int maxHp = 200;
        [SerializeField] int contactDamage = 15;

        [Header("Physics")]
        [SerializeField] Rigidbody2D rb = new();
        [SerializeField] Vector2 initialVelocity = new(-3f, 2f);
        [SerializeField] float maxSpeed = 10f;
        [SerializeField] float minHorizontalSpeed = 1.5f;
        [SerializeField] float horizontalCorrectionForce = 3f;
        
        EnemyEntityModel model;
        readonly CompositeDisposable disposables = new();

        void Reset()
        {
            rb = GetComponent<Rigidbody2D>();
        }

        void Awake()
        {
            model = new EnemyEntityModel(maxHp, contactDamage);
            rb.linearVelocity = initialVelocity;

            model.OnDeath
                .Subscribe(_ => HandleDeath())
                .AddTo(disposables);
        }

        void FixedUpdate()
        {
            if (rb == null) return;
        
            var vel = rb.linearVelocity;
        
            // 横方向の速度が小さすぎる場合、ランダムな横方向の力を加える
            if (Mathf.Abs(vel.x) < minHorizontalSpeed)
            {
                var sign = vel.x >= 0f ? 1f : -1f;
                // ほぼゼロならランダムに方向を決める
                if (Mathf.Abs(vel.x) < 0.1f)
                {
                    sign = UnityEngine.Random.value > 0.5f ? 1f : -1f;
                }
                rb.AddForce(new Vector2(sign * horizontalCorrectionForce, 0f), ForceMode2D.Force);
            }
        
            // 速度制限
            if (rb.linearVelocity.magnitude > maxSpeed)
            {
                rb.linearVelocity = rb.linearVelocity.normalized * maxSpeed;
            }
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (model == null || !model.IsAlive) return;

            var otherPresenter = other.GetComponent<IEntityPresenter>();
            if (otherPresenter != null)
            {
                model.OnCollision(otherPresenter.GetModel());
            }
        }

        void HandleDeath()
        {
            Destroy(gameObject);
        }

        void OnDestroy()
        {
            disposables.Dispose();
            model?.Dispose();
        }

        public EntityBase GetModel() => model;
    }
}
