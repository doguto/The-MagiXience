using UniRx;
using UnityEngine;
using UnityEngine.Pool;
using Project.Scenes.Battle.Scripts.Model;
using Project.Scenes.Battle.Scripts.Model.Entity;
using Project.Scenes.Battle.Scripts.View.Entity;

namespace Project.Scenes.Battle.Scripts.Presenter.Entity
{
    // 弾の見た目・当たり判定・寿命・画面外判定を受け持つ。位置は BulletSystem が毎フレーム与える
    [RequireComponent(typeof(BulletEntityView))]
    [RequireComponent(typeof(SpriteRenderer))]
    public class BulletEntityPresenter : MonoBehaviour, IEntityPresenter
    {
        [SerializeField] BulletEntityView view;
        [SerializeField] SpriteRenderer spriteRenderer;
        [SerializeField, Tooltip("弾のライフタイム（秒）。0以下で無制限")]
        float lifetime = 5f;
        [SerializeField, Min(0f), Tooltip("風の影響の倍率。0で影響なし、1で標準、1より大きいと標準より強く流される")]
        float windInfluence = 1f;

        void Reset()
        {
            view = GetComponent<BulletEntityView>();
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        void Awake()
        {
            // プール再利用時に上書きされる前の、Prefab本来の回転を保持しておく
            baseRotation = transform.rotation;

            // Rigidbody2Dを持たないColliderは静的コライダー扱いとなり、Transformで動かすたびに
            // ブロードフェーズへの再登録が発生して重い。移動する弾はKinematicとして扱わせる。
            if (!TryGetComponent<Rigidbody2D>(out _))
            {
                var body = gameObject.AddComponent<Rigidbody2D>();
                body.bodyType = RigidbodyType2D.Kinematic;
                body.useFullKinematicContacts = false;
                hasRuntimeKinematicBody = true;
            }
        }

        BulletEntityModel model;
        IObjectPool<BulletEntityPresenter> pool;
        Vector3 travelOrigin;
        Vector2 travelAxis;
        float travelRange;
        float lifetimeElapsed;
        float screenMargin;
        bool isInPool;
        bool hasRuntimeKinematicBody;
        Quaternion baseRotation = Quaternion.identity;
        readonly CompositeDisposable disposables = new();

        public BulletEntityModel Model => model;
        public bool IsActive => !isInPool && model != null && model.IsAlive;
        public float WindInfluence => windInfluence;

        /// <param name="rotation">Prefab本来の回転に対するオフセット</param>
        /// <param name="range">飛距離の上限(ワールド単位)。0以下で無制限</param>
        /// <returns>Prefab本来の回転を合成した、実際の回転</returns>
        public Quaternion InitializeDriven(int damage, Vector3 position, Vector2 direction, IObjectPool<BulletEntityPresenter> objectPool, Quaternion rotation, float range, bool isPlayerBullet = false)
        {
            pool = objectPool;
            var offsetRotation = rotation == default ? Quaternion.identity : rotation;
            var resolvedRotation = offsetRotation * baseRotation;
            transform.SetPositionAndRotation(position, resolvedRotation);

            travelOrigin = position;
            travelAxis = direction.normalized;
            travelRange = travelAxis == Vector2.zero ? 0f : range;

            if (model == null)
            {
                model = new BulletEntityModel(damage, isPlayerBullet);
                BindModelToView();
            }
            else
            {
                model.Reinitialize(damage, isPlayerBullet);
            }

            isInPool = false;
            view.ResetView();

            // 画面外判定の余白は毎フレームbounds計算するとコストが高いため生成時に求める。
            // 回転しても外接円に収まるようextentsの長さを使う。
            screenMargin = spriteRenderer.bounds.extents.magnitude + 0.1f;
            lifetimeElapsed = 0f;
            return resolvedRotation;
        }

        // modelはプールから再利用されても同一インスタンスのため、購読は生成時の1回のみ行う
        void BindModelToView()
        {
            model.CurrentHp
                .Where(hp => hp <= 0)
                .Subscribe(_ => ReturnToPool())
                .AddTo(disposables);
        }

        void LateUpdate()
        {
            if (isInPool || model == null || !model.IsAlive) return;

            if (lifetime > 0f)
            {
                lifetimeElapsed += Time.deltaTime;
                if (lifetimeElapsed >= lifetime)
                {
                    ReturnToPool();
                    return;
                }
            }

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
            var margin = screenMargin;

            return position.x < ScreenBoundsCache.MinX - margin || position.x > ScreenBoundsCache.MaxX + margin ||
                   position.y < ScreenBoundsCache.MinY - margin || position.y > ScreenBoundsCache.MaxY + margin;
        }

        public void ReturnToPool()
        {
            // 寿命・画面外・被弾が同フレームに重なった場合の二重返却を防ぐ
            if (isInPool) return;
            isInPool = true;

            if (pool != null)
                pool.Release(this);
            else
                Destroy(gameObject);
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (model == null || !model.IsAlive) return;

            // 実行時に付与したKinematicボディにより、従来は接触判定されなかった静的コライダーとも
            // トリガーが発生するようになるため、従来どおり無視する
            if (hasRuntimeKinematicBody && (other.attachedRigidbody == null || other.attachedRigidbody.bodyType == RigidbodyType2D.Static)) return;

            var otherPresenter = other.GetComponent<IEntityPresenter>();
            if (otherPresenter != null)
                model.OnCollision(otherPresenter.GetModel());
        }

        public void OnReturnedToPool()
        {
            isInPool = true;
            view.SetVisible(false);
            gameObject.SetActive(false);
        }

        public void OnTakenFromPool()
        {
            gameObject.SetActive(true);
        }

        void OnDestroy()
        {
            disposables.Dispose();
            model?.Dispose();
        }

        public EntityBase GetModel() => model;
    }
}
