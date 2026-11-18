using System;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.View
{
    /// <summary>
    /// 4面ビーム本体の見た目。1枚のスプライト(Beam.png)を線分の長さに合わせて横方向へ伸ばし、
    /// フェードイン → 一定時間表示 → フェードアウトして自壊する。
    ///
    /// 2面のように弾を連射せず「1枚絵の表示/非表示」でビームを表現するため、
    /// 起点終点指定のビーム(SpriteBeamAttackSignal)から SpawnAtWorld で生成され、
    /// ConfigureBeam() で length(=射程) と duration(=表示時間) を受け取る。
    ///
    /// スプライトは pivot が右端中央(x=1, y=0.5)で用意されている前提。
    /// localScale.x を「射程 / スプライトの元ワールド幅」の負値に合わせることで、
    /// 起点から終点まできっちり伸びる。当たり判定は SpriteBeamPresenter が別途持つ。
    ///
    /// IBeamVisualReceiver の受け口は同オブジェクトの SpriteBeamPresenter 側に集約している
    /// (1オブジェクトに受け口が2つあると生成側の TryGetComponent が片方しか拾えないため)。
    /// このViewは Presenter から Configure() で駆動される。
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class SpriteBeamView : MonoBehaviour
    {
        [SerializeField, Tooltip("伸縮させるビーム本体のスプライト。未設定なら同オブジェクトのSpriteRendererを使う")]
        SpriteRenderer spriteRenderer;

        [Header("Shape")]
        [SerializeField, Min(0f), Tooltip("射程未指定(0)のときに使う既定の長さ(ワールド単位)")]
        float defaultLength = 14f;

        [SerializeField, Min(0.01f), Tooltip("スプライトの縦の太さ(ワールド単位)。localScale.y に反映する")]
        float thickness = 1f;

        [Header("Animation")]
        [SerializeField, Min(0f), Tooltip("表示時間(秒)。ConfigureBeamのdurationで上書きされる。0で自壊しない")]
        float duration = 1f;

        [SerializeField, Min(0f), Tooltip("フェードインにかける時間(秒)")]
        float fadeInDuration = 0.08f;

        [SerializeField, Min(0f), Tooltip("フェードアウトにかける時間(秒)。表示時間の終盤に消える")]
        float fadeOutDuration = 0.15f;

        [SerializeField, Range(0f, 1f), Tooltip("表示中の最大アルファ")]
        float maxAlpha = 1f;

        float length = -1f;
        float elapsed;
        Color baseColor;

        void Reset()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        void Awake()
        {
            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
            baseColor = spriteRenderer.color;
        }

        void Start()
        {
            ApplyLength();
            ApplyAlpha();
        }

        /// <summary>
        /// ビームの線分に合わせて長さ・表示時間・太さを差し替える。
        /// SpriteBeamPresenter が Instantiate 直後(Start前)に呼ぶ想定だが、
        /// 生成後に呼ばれても伸縮を反映する。
        /// </summary>
        public void Configure(float range, float duration, float width = 0f)
        {
            if (range > 0f) length = range;
            if (duration > 0f) this.duration = duration;
            if (width > 0f) thickness = width;

            if (spriteRenderer != null) ApplyLength();
        }

        void ApplyLength()
        {
            var targetLength = length > 0f ? length : defaultLength;

            // スプライトの元ワールド幅(pixelsPerUnit考慮済み)で割ることで、localScale.x を射程ちょうどにする
            var spriteWidth = spriteRenderer.sprite != null ? spriteRenderer.sprite.bounds.size.x : 1f;
            if (spriteWidth <= 0f) spriteWidth = 1f;

            var scale = transform.localScale;
            // pivotが右端(1, 0.5)なので、localScale.xを負にして起点(敵側)から終点方向へ伸ばす
            scale.x = -(targetLength / spriteWidth);
            scale.y = thickness;
            transform.localScale = scale;
        }

        void Update()
        {
            elapsed += Time.deltaTime;
            ApplyAlpha();

            if (duration > 0f && elapsed >= duration) Destroy(gameObject);
        }

        void ApplyAlpha()
        {
            var alpha = maxAlpha;

            if (fadeInDuration > 0f && elapsed < fadeInDuration)
            {
                alpha = maxAlpha * Mathf.Clamp01(elapsed / fadeInDuration);
            }

            if (duration > 0f && fadeOutDuration > 0f)
            {
                var remaining = duration - elapsed;
                if (remaining < fadeOutDuration)
                {
                    alpha = maxAlpha * Mathf.Clamp01(remaining / fadeOutDuration);
                }
            }

            spriteRenderer.color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
        }
    }
}
