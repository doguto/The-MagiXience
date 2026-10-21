using UnityEngine;
using Project.Scenes.Battle.Scripts.Model.Entity;
using Project.Scenes.Battle.Scripts.View;

namespace Project.Scenes.Battle.Scripts.Presenter.Entity
{
    /// <summary>
    /// 4面ビーム本体の当たり判定とライフサイクルを担う。
    ///
    /// 見た目(SpriteBeamView)と同じGameObjectに載り、生成側(EnemyEntityPresenter)からは
    /// IBeamVisualReceiver の唯一の受け口としてこのPresenterが ConfigureBeam を受ける
    /// (1オブジェクトに受け口が2つあると生成側の TryGetComponent が片方しか拾えないため)。
    /// 受けた range/duration は SpriteBeamView へ転送し、見た目と当たり判定の寿命を揃える。
    ///
    /// 当たり判定は静止した帯状の BoxCollider2D。ビームが「出ている」表示区間だけ有効化し、
    /// OnTriggerStay2D で接触中のプレイヤーへ毎フレーム衝突を伝える。
    /// プレイヤー側の無敵時間(TakeDamageWithInvincibility)により自然に断続ダメージになるため、
    /// ビーム内に居続けるプレイヤーは無敵が切れるたびに再びダメージを受ける。
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class SpriteBeamPresenter : MonoBehaviour, IBeamVisualReceiver, IEntityPresenter
    {
        [SerializeField] SpriteBeamView view;
        [SerializeField] BoxCollider2D beamCollider;

        [SerializeField, Min(0), Tooltip("ビームに触れているプレイヤーへ与える接触ダメージ")]
        int contactDamage = 15;

        [SerializeField, Min(0f), Tooltip("表示時間(秒)。ConfigureBeamのdurationで上書きされる。0で自壊しない")]
        float duration = 1f;

        [SerializeField, Min(0f), Tooltip("生成から当たり判定が有効になるまでの遅延(秒)。フェードインと揃えると自然")]
        float hitDelay = 0.08f;

        [SerializeField, Min(0f), Tooltip("表示終了の何秒前に当たり判定を切るか。フェードアウトと揃えると自然")]
        float hitEndMargin = 0.15f;

        // maxHp は使わないが EnemyEntityModel の生成に必要。撃破されない前提で1にしておく
        EnemyEntityModel model;
        float elapsed;

        void Reset()
        {
            view = GetComponent<SpriteBeamView>();
            beamCollider = GetComponent<BoxCollider2D>();
        }

        void Awake()
        {
            if (beamCollider == null) beamCollider = GetComponent<BoxCollider2D>();
            if (view == null) view = GetComponent<SpriteBeamView>();

            model = new EnemyEntityModel(1, contactDamage);

            // 有効化は表示区間に入ってから。生成直後は判定を持たせない
            beamCollider.enabled = false;
        }

        /// <summary>
        /// 生成側から線分の長さと表示時間を受け取り、Viewへ転送する。
        /// Start前に呼ばれる想定。
        /// </summary>
        public void ConfigureBeam(float range, float duration, float width = 0f)
        {
            if (duration > 0f) this.duration = duration;

            if (view != null) view.Configure(range, duration, width);
        }

        void Update()
        {
            elapsed += Time.deltaTime;

            var active = elapsed >= hitDelay &&
                         (duration <= 0f || elapsed < duration - hitEndMargin);
            if (beamCollider.enabled != active) beamCollider.enabled = active;

            if (duration > 0f && elapsed >= duration) Destroy(gameObject);
        }

        void OnTriggerStay2D(Collider2D other)
        {
            if (!beamCollider.enabled) return;

            var otherPresenter = other.GetComponent<IEntityPresenter>();
            if (otherPresenter == null) return;

            // プレイヤー側の無敵時間があるので、毎フレーム叩いても連続では入らない
            otherPresenter.GetModel().OnCollision(model);
        }

        public EntityBase GetModel() => model;

        void OnDestroy()
        {
            model?.Dispose();
        }
    }
}
