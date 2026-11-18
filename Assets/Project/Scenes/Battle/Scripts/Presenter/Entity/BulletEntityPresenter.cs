using UniRx;
using UnityEngine;
using UnityEngine.Pool;
using Project.Scenes.Battle.Scripts.Model.Entity;
using Project.Scenes.Battle.Scripts.View.Entity;

namespace Project.Scenes.Battle.Scripts.Presenter.Entity
{
    // 弾の見た目と当たり判定を受け持つ。位置・消えるかの判定・片付けは BulletSystem が行う
    [RequireComponent(typeof(BulletEntityView))]
    [RequireComponent(typeof(SpriteRenderer))]
    public class BulletEntityPresenter : MonoBehaviour, IEntityPresenter
    {
        [SerializeField] BulletEntityView view;
        [SerializeField] SpriteRenderer spriteRenderer;
        [SerializeField, Min(0f), Tooltip("ステージの外力の影響の倍率。0で影響なし、1で標準、1より大きいと標準より強く流される")]
        float forceInfluence = 1f;

        void Reset()
        {
            view = GetComponent<BulletEntityView>();
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        void Awake()
        {
            // プール再利用時に上書きされる前の、Prefab本来の回転を保持しておく
            baseRotation = transform.rotation;
            colliders = GetComponents<Collider2D>();

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
        Collider2D[] colliders;
        float screenMargin;
        bool isInPool;
        bool hasRuntimeKinematicBody;
        Quaternion baseRotation = Quaternion.identity;
        readonly CompositeDisposable disposables = new();

        public BulletEntityModel Model => model;
        // 何かに当たって消費された。片付けは次のフレームに BulletSystem が行う
        public bool IsConsumed => model != null && !model.IsAlive;
        public float ForceInfluence => forceInfluence;
        // 画面外とみなすまでの余白。回転しても外接円に収まるよう extents の長さを使う
        public float ScreenMargin => screenMargin;

        /// <param name="rotation">Prefab本来の回転に対するオフセット</param>
        /// <returns>Prefab本来の回転を合成した、実際の回転</returns>
        public Quaternion InitializeDriven(int damage, Vector3 position, IObjectPool<BulletEntityPresenter> objectPool, Quaternion rotation, bool isPlayerChargeBullet = false)
        {
            pool = objectPool;
            var offsetRotation = rotation == default ? Quaternion.identity : rotation;
            var resolvedRotation = offsetRotation * baseRotation;
            transform.SetPositionAndRotation(position, resolvedRotation);

            if (model == null)
            {
                model = new BulletEntityModel(damage, isPlayerChargeBullet);
                BindModelToView();
            }
            else
            {
                model.Reinitialize(damage, isPlayerChargeBullet);
            }

            isInPool = false;
            view.ResetView();
            SetCollidersEnabled(true);

            // 毎フレーム bounds を計算するとコストが高いため生成時に求める
            screenMargin = spriteRenderer.bounds.extents.magnitude + 0.1f;
            return resolvedRotation;
        }

        // modelはプールから再利用されても同一インスタンスのため、購読は生成時の1回のみ行う
        void BindModelToView()
        {
            model.CurrentHp
                .Where(hp => hp <= 0)
                .Subscribe(_ => HideConsumed())
                .AddTo(disposables);
        }

        // 片付けまでの間に他のものへ当たったり見えたりしないようにする
        void HideConsumed()
        {
            SetCollidersEnabled(false);
            view.SetVisible(false);
        }

        void SetCollidersEnabled(bool value)
        {
            foreach (var c in colliders) c.enabled = value;
        }

        public void ReturnToPool()
        {
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
