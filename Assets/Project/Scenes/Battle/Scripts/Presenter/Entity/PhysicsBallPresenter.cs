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
        [SerializeField] Vector2 initialVelocity = new(-3f, 2f);
        [SerializeField] float gravityScale = 0.5f;
        [SerializeField] float bounciness = 1f;
        [SerializeField] float friction = 0f;
        [SerializeField] float maxSpeed = 10f;
        [SerializeField] float minHorizontalSpeed = 1.5f;
        [SerializeField] float horizontalCorrectionForce = 3f;

        [Header("Collider")]
        [SerializeField] float radius = 0.3f;

        EnemyEntityModel model;
        Rigidbody2D rb;
        readonly CompositeDisposable disposables = new();

        void Awake()
        {
            model = new EnemyEntityModel(maxHp, contactDamage);
            SetupPhysics();
            SetupColliders();

            model.OnDeath
                .Subscribe(_ => HandleDeath())
                .AddTo(disposables);
        }

        void SetupPhysics()
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = gravityScale;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;

            var material = new PhysicsMaterial2D("PhysicsBallMaterial")
            {
                bounciness = bounciness,
                friction = friction
            };
            rb.sharedMaterial = material;

            rb.linearVelocity = initialVelocity;
        }

        void SetupColliders()
        {
            // Solid: 壁やスペクトラムバーとの物理反射用
            var solidCollider = gameObject.AddComponent<CircleCollider2D>();
            solidCollider.isTrigger = false;
            solidCollider.radius = radius;

            // Trigger: プレイヤーとの接触ダメージ検出用（少し大きめ）
            var triggerCollider = gameObject.AddComponent<CircleCollider2D>();
            triggerCollider.isTrigger = true;
            triggerCollider.radius = radius * 1.1f;
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
                    sign = Random.value > 0.5f ? 1f : -1f;
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
            Debug.Log($"[PhysicsBallPresenter] Ball destroyed at {transform.position}");
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
