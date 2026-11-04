using DG.Tweening;
using Project.Scenes.Battle.Scripts.Model;
using Project.Scenes.Battle.Scripts.Model.Movement;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Presenter.Entity
{
    /// <summary>Stage3道中Phase5の双子中ボス。個別のHPで3行動を繰り返し、本ボス用シーケンスは使わない。</summary>
    [DisallowMultipleComponent, RequireComponent(typeof(EnemyEntityPresenter))]
    public class TwinParabolaMidBossController : MonoBehaviour
    {
        [Header("Role / Bullet Pools")]
        [SerializeField, Tooltip("青はON（最初は上）、赤はOFF（最初は下）")]
        bool isBlue = true;
        [SerializeField] BulletPool normalBulletPool;
        [SerializeField] BulletPool parabolaBulletPool;
        [SerializeField, Min(1)] int bulletDamage = 10;

        [Header("Positions (world coordinates)")]
        [SerializeField] float rightX = 6.3f;
        [SerializeField] float leftX = -6.3f;
        [SerializeField] float centerY;
        [SerializeField, Min(0.1f)] float rowOffset = 2.5f;
        [SerializeField, Min(0.01f), Tooltip("行動3で上下の画像の間に残す距離")]
        float centerGap = 0.12f;

        [Header("Timing (seconds)")]
        [SerializeField, Min(0.1f)] float entranceDuration = 1.2f;
        [SerializeField, Min(0.1f)] float diagonalDuration = 4f;
        [SerializeField, Min(0.1f)] float returnToRightDuration = 1.2f;
        [SerializeField, Min(0.1f)] float doubleShowerDuration = 3f;
        [SerializeField, Min(0.1f)] float gatherDuration = 1f;
        [SerializeField, Min(0.1f), Tooltip("行動3全体の秒数。開始→折り返し→開始→最後の3区間に等分する。")]
        float sweepingShowerDuration = 6f;
        [SerializeField, Min(0.1f)] float resetDuration = 1.2f;

        [Header("Parabola / Yellow Shot")]
        [SerializeField, Min(0.05f)] float shotInterval = 0.25f;
        [SerializeField, Min(0.1f)] float parabolaSpeed = 8f;
        [SerializeField] float normalVertexX = -6f;
        [SerializeField, Min(0.1f)] float vertexDepth = 4f;
        [SerializeField] float sweepNearX = -3f;
        [SerializeField] float sweepFarX = -10f;
        [SerializeField, Tooltip("行動3の最後の相対頂点X。正なら右側へ撃つ。開始→折り返し→開始→最後の順に変化する。")]
        float sweepEndX = 4f;
        [SerializeField, Min(0.05f), Tooltip("行動3で右側へ撃つときの頂点と画面端の間隔（ワールド単位）")]
        float sweepEdgeMargin = 0.2f;
        [SerializeField, Min(0.1f)] float aimedShotSpeed = 6f;
        [SerializeField, Min(0.1f)] float vertexScreenMargin = 0.7f;

        EnemyEntityPresenter enemy;
        Transform player;
        Vector3 spawnPosition;
        float elapsed;
        float halfHeight;
        long nextShot;
        int nextAimedCycle;

        float CycleDuration => diagonalDuration + returnToRightDuration + doubleShowerDuration
            + gatherDuration + sweepingShowerDuration + resetDuration;

        void Reset() => FindPools();

        void FindPools()
        {
            foreach (var pool in GetComponentsInChildren<BulletPool>(true))
            {
                if (pool.name == "LinearBulletPool") normalBulletPool = pool;
                if (pool.name == "ParabolaBulletPool") parabolaBulletPool = pool;
            }
        }

        void OnValidate()
        {
            entranceDuration = Mathf.Max(0.1f, entranceDuration);
            diagonalDuration = Mathf.Max(0.1f, diagonalDuration);
            returnToRightDuration = Mathf.Max(0.1f, returnToRightDuration);
            doubleShowerDuration = Mathf.Max(0.1f, doubleShowerDuration);
            gatherDuration = Mathf.Max(0.1f, gatherDuration);
            sweepingShowerDuration = Mathf.Max(0.1f, sweepingShowerDuration);
            resetDuration = Mathf.Max(0.1f, resetDuration);
            shotInterval = Mathf.Max(0.05f, shotInterval);
            sweepEdgeMargin = Mathf.Max(0.05f, sweepEdgeMargin);
        }

        void Start()
        {
            enemy = GetComponent<EnemyEntityPresenter>();
            player = PlayerPositionReference.Transform;
            if (normalBulletPool == null || parabolaBulletPool == null) FindPools();
            if (normalBulletPool == null || parabolaBulletPool == null)
            {
                Debug.LogError("[TwinParabolaMidBoss] Assign normal and parabola bullet pools.", this);
                enabled = false;
                return;
            }
            spawnPosition = transform.position;
            halfHeight = GetComponent<SpriteRenderer>().bounds.extents.y;
            // 通常のEnemyのHP・死亡・Tracker登録をそのまま使う。寿命で退場させない。
        }

        void LateUpdate()
        {
            if (enemy == null || enemy.Model == null || !enemy.Model.IsAlive) return;
            elapsed += Time.deltaTime;
            if (elapsed < entranceDuration)
            {
                SetPosition(Vector2.Lerp(spawnPosition, Row(true), Smooth(elapsed / entranceDuration)));
                return;
            }

            float attackTime = elapsed - entranceDuration;
            // フレーム落ちでも中央通過の黄色弾を飛ばさない。各周回・各ボスにつき1発。
            while (nextAimedCycle * CycleDuration + diagonalDuration * 0.5f <= attackTime)
            {
                SetPosition(PositionAt(diagonalDuration * 0.5f));
                FireAimedShot();
                nextAimedCycle++;
            }
            // 発射位置も予定時刻から算出し、移動と攻撃を同じ時計で合わせる。
            while ((nextShot + 0.5) * shotInterval <= attackTime)
            {
                float shotTime = (float)((nextShot + 0.5) * shotInterval);
                FireParabolas(Mathf.Repeat(shotTime, CycleDuration));
                nextShot++;
            }
            SetPosition(PositionAt(Mathf.Repeat(attackTime, CycleDuration)));
        }

        Vector2 Row(bool initial) => new(rightX, centerY + (isBlue == initial ? rowOffset : -rowOffset));
        Vector2 EndOfDiagonal => new(leftX, Row(false).y);
        Vector2 Gathered => new(rightX, centerY + (isBlue ? -1f : 1f) * (halfHeight + centerGap * 0.5f));
        static float Smooth(float t) => Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t));

        Vector2 PositionAt(float t)
        {
            if (t < diagonalDuration)
                return Vector2.Lerp(Row(true), EndOfDiagonal, t / diagonalDuration);
            t -= diagonalDuration;
            if (t < returnToRightDuration)
                return Vector2.Lerp(EndOfDiagonal, Row(false), Smooth(t / returnToRightDuration));
            t -= returnToRightDuration;
            if (t < doubleShowerDuration) return Row(false);
            t -= doubleShowerDuration;
            if (t < gatherDuration)
                return Vector2.Lerp(Row(false), Gathered, Smooth(t / gatherDuration));
            t -= gatherDuration;
            if (t < sweepingShowerDuration) return Gathered;
            t -= sweepingShowerDuration;
            // 外側を回って初期の上下へ戻す。画像が中央で重ならないようXも分ける。
            Vector2 resetPosition = Vector2.Lerp(Gathered, Row(true), Smooth(t / resetDuration));
            resetPosition.x += (isBlue ? -1f : 1f) * Mathf.Sin(Mathf.PI * Mathf.Clamp01(t / resetDuration));
            return resetPosition;
        }

        void SetPosition(Vector2 position) => transform.position = new Vector3(position.x, position.y, spawnPosition.z);

        void FireParabolas(float t)
        {
            SetPosition(PositionAt(t));
            if (t < diagonalDuration)
            {
                // Phase20と同じ(-6, ±4)、速さ8を基準に、今いる側の内向きへ撃つ。
                FireParabola(normalVertexX, transform.position.y >= centerY ? -vertexDepth : vertexDepth);
                return;
            }
            t -= diagonalDuration + returnToRightDuration;
            if (t >= 0f && t < doubleShowerDuration)
            {
                // 外向きの頂点も画面端の内側に収める。弾が折り返す前に画面外へ出ない。
                float y = parabolaBulletPool.transform.position.y;
                float upY = Mathf.Min(y + vertexDepth, ScreenBoundsCache.MaxY - vertexScreenMargin);
                float downY = Mathf.Max(y - vertexDepth, ScreenBoundsCache.MinY + vertexScreenMargin);
                FireParabola(normalVertexX, upY - y);
                FireParabola(normalVertexX, downY - y);
                return;
            }
            t -= doubleShowerDuration + gatherDuration;
            if (t >= 0f && t < sweepingShowerDuration)
            {
                float segment = t / sweepingShowerDuration * 3f;
                // -3→-10→-3→+4を等時間で移動し、戻りだけが速くならないようにする。
                float vertexX = segment < 1f
                    ? Mathf.Lerp(sweepNearX, sweepFarX, Smooth(segment))
                    : segment < 2f
                        ? Mathf.Lerp(sweepFarX, sweepNearX, Smooth(segment - 1f))
                        : Mathf.Lerp(sweepNearX, sweepEndX, Smooth(segment - 2f));
                Vector2 origin = parabolaBulletPool.transform.position;
                float edgeY = isBlue ? ScreenBoundsCache.MaxY - sweepEdgeMargin
                    : ScreenBoundsCache.MinY + sweepEdgeMargin;
                float initialY = origin.y + (isBlue ? vertexDepth : -vertexDepth);
                // 頂点Xが正になるにつれて、上下端付近まで頂点を伸ばして右の角を掃く。
                float rightSweep = sweepEndX > 0f ? Smooth(Mathf.InverseLerp(0f, sweepEndX, vertexX)) : 0f;
                float vertexY = Mathf.Lerp(initialY, edgeY, rightSweep) - origin.y;
                FireParabola(vertexX, vertexY);
            }
        }

        void FireParabola(float vertexX, float vertexY)
        {
            // 左右を切り替える瞬間、頂点X=0の退化した放物線は発射しない。
            if (Mathf.Abs(vertexX) < 0.01f || Mathf.Abs(vertexY) < 0.01f) return;
            parabolaBulletPool.SpawnBullet(bulletDamage, parabolaBulletPool.transform.position,
                movementOverride: new VertexParabolaMovementConfig(new Vector2(vertexX, vertexY), parabolaSpeed));
        }

        void FireAimedShot()
        {
            if (player == null) player = PlayerPositionReference.Transform;
            Vector3 origin = normalBulletPool.transform.position;
            Vector2 direction = player != null ? (Vector2)(player.position - origin).normalized : Vector2.left;
            normalBulletPool.SpawnBullet(bulletDamage, origin, direction,
                movementOverride: new ConstantVelocity(direction * aimedShotSpeed));
        }

        sealed class ConstantVelocity : IMovementStep
        {
            readonly Vector2 velocity;
            public ConstantVelocity(Vector2 velocity) => this.velocity = velocity;

            public Tween Play(Transform target, Vector2 direction, Animator animator)
            {
                Vector3 start = target.position;
                return DOVirtual.Float(0f, 999999f, 999999f,
                    time => target.position = start + (Vector3)(velocity * time)).SetEase(Ease.Linear);
            }
        }
    }
}
