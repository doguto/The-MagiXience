using Project.Scenes.Battle.Scripts.Model;
using Project.Scenes.Battle.Scripts.Model.Movement;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Presenter.Entity
{
    /// <summary>Stage3本ボス専用。HP・強化切替・撃破は既存のBossEntityPresenterが管理する。</summary>
    [DisallowMultipleComponent, RequireComponent(typeof(BossEntityPresenter))]
    public class Stage3WindBossController : MonoBehaviour
    {
        [Header("Bullet Pools")]
        [SerializeField] BulletPool normalBulletPool;
        [SerializeField] BulletPool parabolaBulletPool;
        [SerializeField, Min(1)] int bulletDamage = 10;
        [Header("Wind")]
        [SerializeField, Min(0.1f)] float cueSeconds = 0.9f;
        [SerializeField, Range(0f, 1f)] float playerSpeedRatio = 0.2f;
        [SerializeField, Min(0f)] float bulletAcceleration = 2f;
        [SerializeField, Min(0f)] float bulletDrag = 0.25f;
        [Header("Arm Raise Cue")]
        [SerializeField] Sprite raisedArmsSprite;
        [Header("Attack")]
        [SerializeField, Min(0.05f)] float normalInterval = 0.3f;
        [SerializeField, Min(0.05f)] float strongInterval = 0.2f;
        [SerializeField, Min(0.1f)] float parabolaSpeed = 8f;
        [SerializeField, Min(0.1f)] float normalSpeed = 6f;

        BossEntityPresenter boss;
        SpriteRenderer spriteRenderer;
        Sprite idleSprite;
        Vector3 idleScale;
        Vector3 home;
        bool hasHome;
        bool active;
        bool strong;
        bool parabolaSection;
        int section;
        int shot;
        float elapsed;
        float nextShot;
        WindDirection direction;

        void Reset() => FindPools();

        void FindPools()
        {
            foreach (var pool in GetComponentsInChildren<BulletPool>(true))
            {
                if (pool.name == "LinearBulletPool") normalBulletPool = pool;
                if (pool.name == "ParabolaBulletPool") parabolaBulletPool = pool;
            }
        }

        void Awake()
        {
            boss = GetComponent<BossEntityPresenter>();
            spriteRenderer = GetComponent<SpriteRenderer>();
            idleSprite = spriteRenderer.sprite;
            idleScale = transform.localScale;
            if (normalBulletPool == null || parabolaBulletPool == null) FindPools();
            boss.PhaseStarted += BeginSection;
        }

        void BeginSection(BattlePhaseModelBase phase, BattleTimelineBuilderAsset builder)
        {
            if (builder == null) { active = false; return; }
            if (normalBulletPool == null || parabolaBulletPool == null)
            {
                Debug.LogError("[Stage3WindBoss] Assign normal and parabola pools.", this);
                return;
            }
            if (!hasHome) { home = transform.position; hasHome = true; }
            strong = boss.Model.ShouldUseStrongAttack;
            parabolaSection = section % 2 == 0;
            int windIndex = section % (strong ? 4 : 3);
            direction = strong
                ? new[] { WindDirection.Up, WindDirection.Left, WindDirection.Down, WindDirection.Right }[windIndex]
                : new[] { WindDirection.Up, WindDirection.Down, WindDirection.Right }[windIndex];
            elapsed = 0f;
            nextShot = cueSeconds;
            shot = 0;
            section++;
            active = true;
            Debug.Log($"[Stage3WindBoss] Section={section}, Strong={strong}, Pattern={(parabolaSection ? "Parabola" : "NWay")}, Wind={direction}", this);
            // Keep the previous wind throughout the warning, including its velocity/drag effects.
            // The first section starts with its own wind rather than a windless interval.
            var cueDirection = BattleWind.Direction;
            if (cueDirection == WindDirection.None) cueDirection = direction;
            BattleWind.Begin(new BattleWindSettings
            {
                enabled = true,
                playerSpeedRatio = playerSpeedRatio,
                bulletAcceleration = bulletAcceleration,
                bulletDrag = bulletDrag,
                intervals = new()
                {
                    new WindInterval { direction = cueDirection, seconds = cueSeconds },
                    new WindInterval { direction = direction, seconds = 3600f }
                }
            });
        }

        void Update()
        {
            if (!active || boss.Model == null || !boss.Model.IsAlive) return;
            elapsed += Time.deltaTime;
            bool cue = elapsed < cueSeconds;
            if (raisedArmsSprite != null) spriteRenderer.sprite = cue ? raisedArmsSprite : idleSprite;
            // A small body lift remains visible when the optional raised-arm frame is unavailable.
            float lift = cue ? Mathf.Sin(Mathf.PI * elapsed / cueSeconds) : 0f;
            transform.localScale = idleScale + Vector3.Scale(idleScale, new Vector3(-0.04f, 0.08f, 0f)) * lift;
            transform.position = home + new Vector3(0f, cue ? lift * 0.18f : Mathf.Sin((elapsed - cueSeconds) * 0.65f) * 1.4f, 0f);
            int catchUp = 0;
            while (elapsed >= nextShot && catchUp++ < 4)
            {
                Fire(nextShot - cueSeconds);
                nextShot += strong ? Mathf.Max(0.05f, strongInterval) : Mathf.Max(0.05f, normalInterval);
                shot++;
            }
        }

        void Fire(float time)
        {
            if (parabolaSection)
            {
                float vertexX = -5.5f + 2.5f * Mathf.Sin(time * 1.5f);
                float depth = 2.6f + 1.2f * Mathf.Sin(time * 0.8f);
                for (int sign = -1; sign <= 1; sign += 2)
                {
                    var movement = new VertexParabolaMovementConfig(new Vector2(vertexX, sign * depth), parabolaSpeed);
                    parabolaBulletPool.SpawnBullet(bulletDamage, transform.position, Vector2.left, movementOverride: movement);
                }
                if (strong && shot % 3 == 0) FireFan(transform.position, Vector2.left, 3, 60f);
            }
            else
            {
                var player = PlayerPositionReference.Transform;
                Vector2 aim = player != null ? (Vector2)(player.position - transform.position).normalized : Vector2.left;
                FireFan(transform.position, aim, strong ? 7 : 5, strong ? 100f : 80f);
            }
            if (strong && shot % 5 == 0)
            {
                float y = Mathf.Lerp(ScreenBoundsCache.MinY + 0.6f, ScreenBoundsCache.MaxY - 0.6f,
                    (Mathf.Sin(time * 1.7f) + 1f) * 0.5f);
                // Enter from the left during right wind; other winds use the opposite side.
                bool fromLeft = direction == WindDirection.Right;
                Vector3 origin = new(fromLeft ? ScreenBoundsCache.MinX + 0.02f : ScreenBoundsCache.MaxX - 0.02f, y, transform.position.z);
                FireFan(origin, fromLeft ? Vector2.right : Vector2.left, 3, 24f);
            }
        }

        void FireFan(Vector3 origin, Vector2 direction, int count, float spread)
        {
            for (int i = 0; i < count; i++)
            {
                float angle = count > 1 ? Mathf.Lerp(-spread * 0.5f, spread * 0.5f, (float)i / (count - 1)) : 0f;
                Vector2 velocity = Quaternion.Euler(0f, 0f, angle) * direction;
                normalBulletPool.SpawnBullet(bulletDamage, origin, velocity,
                    movementOverride: new InfiniteMovementConfig(velocity * normalSpeed));
            }
        }

        void OnDestroy()
        {
            if (boss != null) boss.PhaseStarted -= BeginSection;
        }
    }
}
