using System.Collections.Generic;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.View
{
    /// <summary>
    /// 攻撃の予告線。自分の transform.right 方向に点線を伸ばし、
    /// duration 経過後に自壊する。EnemySpawnAttackSignal で生成される想定のため、
    /// EnemyEntityPresenter は付けないこと(付けると EnemyTracker に敵として数えられる)。
    ///
    /// dotSprite 未設定でも実行時にフォールバック形状(既定はChevron="&gt;"の山形)を生成して動くので、
    /// GameObject に本コンポーネントを付けるだけで予告線として成立する。
    /// Chevron + flowSpeed(既定値) の組み合わせで「&lt;&lt;&lt;&lt;&lt;&lt;&lt;」のように進行方向へ流れる矢印列に見える。
    ///
    /// 起点終点指定のビーム(BeamAttackSignal)から生成された場合は、
    /// ConfigureBeam() で length と duration が上書きされる。
    /// transform の回転は BeamLine.Rotation で始点→終点方向に既に揃えられているため、
    /// このコンポーネント側は常に自分の +X(transform.right) 方向へ伸ばすだけでよい。
    /// </summary>
    public class WarningLineView : MonoBehaviour, IBeamVisualReceiver
    {
        /// <summary>点1つの形状。Chevron は進行方向(+X)を指す山形「&gt;」で、flowSpeed と組み合わせて「&lt;&lt;&lt;&lt;&lt;&lt;&lt;」のように流れる矢印列に見せる</summary>
        public enum DotShape
        {
            Chevron,
            Square,
        }

        [Header("Shape")]
        [SerializeField, Tooltip("点の形状。未設定(dotSprite無し)時のみ有効")]
        DotShape dotShape = DotShape.Chevron;

        [SerializeField, Tooltip("設定するとdotShapeより優先される")]
        Sprite dotSprite;

        [SerializeField, Min(0f), Tooltip("線の長さ(ワールド単位)")]
        float length = 14f;

        [SerializeField, Min(0.01f), Tooltip("点の間隔(ワールド単位)")]
        float dotSpacing = 0.4f;

        [SerializeField, Min(0.01f), Tooltip("点1つの大きさ(ワールド単位)")]
        float dotSize = 0.12f;

        [SerializeField, Min(0f), Tooltip("線の太さ(進行方向に直交、ワールド単位)。0以下なら dotSize を流用(従来通りの正方形の点になる)")]
        float width;

        [SerializeField, Min(0f), Tooltip("原点から最初の点までの距離")]
        float startOffset = 0.5f;

        [Header("Appearance")]
        [SerializeField] Color color = new(1f, 0.92f, 0.25f, 0.75f);
        [SerializeField] string sortingLayerName = "Default";
        [SerializeField] int sortingOrder = 5;

        [Header("Arrow")]
        [SerializeField, Tooltip("未設定なら三角形を実行時生成する")]
        Sprite arrowSprite;

        [SerializeField, Min(0f), Tooltip("終点に表示する矢印の長さ(ワールド単位)。0で矢印なし")]
        float arrowLength;

        [SerializeField, Min(0.01f), Tooltip("矢印の幅(ワールド単位)")]
        float arrowWidth = 0.4f;

        [Header("Animation")]
        [SerializeField, Min(0f), Tooltip("表示時間(秒)。0で自壊しない")]
        float duration = 1f;

        [SerializeField, Min(0f), Tooltip("点滅の周期(秒)。0で点滅なし")]
        float blinkInterval = 0.14f;

        [SerializeField, Tooltip("終盤で点滅を加速させる倍率。1で加速なし")]
        float blinkAccelerateRate = 3f;

        [SerializeField, Tooltip("点が奥へ流れる速度(ワールド単位/秒)。負で手前へ流れる")]
        float flowSpeed = 3f;

        [SerializeField, Min(0f), Tooltip("フェードインにかける時間(秒)")]
        float fadeInDuration = 0.15f;

        static Sprite fallbackSquareSprite;
        static Sprite fallbackChevronSprite;
        static Sprite fallbackArrowSprite;

        readonly List<SpriteRenderer> dots = new();
        SpriteRenderer arrowRenderer;
        float elapsed;
        float blinkTimer;
        bool blinkOn = true;

        void Start()
        {
            BuildDots();
            BuildArrow();
        }

        /// <summary>
        /// ビームの線分に合わせて長さ・表示時間・太さを差し替える。
        /// 通常は Instantiate 直後(Start前)に呼ばれるが、生成後に呼ばれても破綻しないよう点を作り直す。
        /// </summary>
        public void ConfigureBeam(float range, float duration, float width = 0f)
        {
            if (range > 0f) length = range;
            if (duration > 0f) this.duration = duration;
            if (width > 0f) this.width = width;

            if (dots.Count > 0) RebuildDots();
        }

        void RebuildDots()
        {
            foreach (var dot in dots)
            {
                if (dot != null) Destroy(dot.gameObject);
            }
            dots.Clear();

            if (arrowRenderer != null)
            {
                Destroy(arrowRenderer.gameObject);
                arrowRenderer = null;
            }

            BuildDots();
            BuildArrow();
        }

        void BuildDots()
        {
            var sprite = dotSprite != null ? dotSprite : GetFallbackSprite(dotShape);
            var dotWidth = width > 0f ? width : dotSize;
            var count = Mathf.Max(1, Mathf.FloorToInt((length - startOffset) / dotSpacing) + 1);

            for (var i = 0; i < count; i++)
            {
                var dot = new GameObject($"Dot{i}");
                dot.transform.SetParent(transform, false);
                dot.transform.localPosition = new Vector3(startOffset + dotSpacing * i, 0f, 0f);

                var renderer = dot.AddComponent<SpriteRenderer>();
                renderer.sprite = sprite;
                renderer.color = color;
                renderer.sortingLayerName = sortingLayerName;
                renderer.sortingOrder = sortingOrder;
                // スプライトは 1x1 ワールド単位で生成しているので、localScale がそのまま点の大きさになる。
                // X=dotSize(進行方向) / Y=dotWidth(太さ) を独立させて、線の太さだけを変えられるようにする
                dot.transform.localScale = new Vector3(dotSize, dotWidth, 1f);

                dots.Add(renderer);
            }
        }

        /// <summary>
        /// 終点(length の位置)を頂点とした三角形を配置し、始点→終点の向きを矢印として示す。
        /// arrowLength が 0 以下なら何も生成しない。
        /// </summary>
        void BuildArrow()
        {
            if (arrowLength <= 0f) return;

            var sprite = arrowSprite != null ? arrowSprite : GetFallbackArrowSprite();

            var arrow = new GameObject("Arrow");
            arrow.transform.SetParent(transform, false);
            // ピボットが底辺中央(左端)なので、底辺の位置に置けば頂点はちょうど length(終点)に届く
            arrow.transform.localPosition = new Vector3(length - arrowLength, 0f, 0f);
            arrow.transform.localScale = new Vector3(arrowLength, arrowWidth, 1f);

            var renderer = arrow.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingLayerName = sortingLayerName;
            renderer.sortingOrder = sortingOrder;

            arrowRenderer = renderer;
        }

        void Update()
        {
            elapsed += Time.deltaTime;

            UpdateBlink();
            UpdateFlow();
            ApplyAlpha();

            if (duration > 0f && elapsed >= duration) Destroy(gameObject);
        }

        void UpdateBlink()
        {
            if (blinkInterval <= 0f)
            {
                blinkOn = true;
                return;
            }

            // 残り時間が短いほど点滅を速くして「来るぞ」感を出す
            var progress = duration > 0f ? Mathf.Clamp01(elapsed / duration) : 0f;
            var rate = Mathf.Lerp(1f, Mathf.Max(1f, blinkAccelerateRate), progress);
            var interval = blinkInterval / rate;

            blinkTimer += Time.deltaTime;
            if (blinkTimer < interval) return;

            blinkTimer -= interval;
            blinkOn = !blinkOn;
        }

        void UpdateFlow()
        {
            if (Mathf.Approximately(flowSpeed, 0f)) return;

            var span = dotSpacing * dots.Count;
            if (span <= 0f) return;

            for (var i = 0; i < dots.Count; i++)
            {
                var baseX = startOffset + dotSpacing * i;
                // 点線全体を dotSpacing 単位で循環させるので、見た目は「流れ続ける点線」になる
                var offset = Mathf.Repeat(flowSpeed * elapsed, dotSpacing);
                dots[i].transform.localPosition = new Vector3(baseX + offset, 0f, 0f);
            }
        }

        void ApplyAlpha()
        {
            var fade = fadeInDuration > 0f ? Mathf.Clamp01(elapsed / fadeInDuration) : 1f;
            var alpha = color.a * fade * (blinkOn ? 1f : 0.25f);
            var applied = new Color(color.r, color.g, color.b, alpha);

            foreach (var dot in dots)
            {
                if (dot != null) dot.color = applied;
            }

            // 矢印は流れないが、点線と同じフェード/点滅には追従させて統一感を出す
            if (arrowRenderer != null) arrowRenderer.color = applied;
        }

        static Sprite GetFallbackSprite(DotShape shape) => shape switch
        {
            DotShape.Chevron => GetFallbackChevronSprite(),
            _ => GetFallbackSquareSprite(),
        };

        static Sprite GetFallbackSquareSprite()
        {
            if (fallbackSquareSprite != null) return fallbackSquareSprite;

            var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                hideFlags = HideFlags.HideAndDontSave
            };
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();

            // pixelsPerUnit=1 なので 1x1 テクスチャが 1x1 ワールド単位になる
            fallbackSquareSprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
            fallbackSquareSprite.hideFlags = HideFlags.HideAndDontSave;
            return fallbackSquareSprite;
        }

        /// <summary>
        /// 進行方向(+X, transform.right)を指す山形「&gt;」を1点分として生成する。
        /// dots を dotSpacing 間隔で並べ、flowSpeed で +X へ流すと「&lt;&lt;&lt;&lt;&lt;&lt;&lt;」のように連続した矢印列に見える。
        /// </summary>
        static Sprite GetFallbackChevronSprite()
        {
            if (fallbackChevronSprite != null) return fallbackChevronSprite;

            const int size = 32;
            const float strokeHalfWidth = 0.16f;
            const float maxRailOffset = 0.42f;

            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave
            };

            for (var x = 0; x < size; x++)
            {
                var u = x / (float)(size - 1);
                // u=0(後方)で上下に開き、u=1(+X, 進行方向)で1点に収束する2本のレールを描く
                var railOffset = (1f - u) * maxRailOffset;

                for (var y = 0; y < size; y++)
                {
                    var v = y / (float)(size - 1) - 0.5f;
                    var onUpperRail = Mathf.Abs(v - railOffset) <= strokeHalfWidth;
                    var onLowerRail = Mathf.Abs(v + railOffset) <= strokeHalfWidth;
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, onUpperRail || onLowerRail ? 1f : 0f));
                }
            }
            texture.Apply();

            // pivotは中心。localScale(dotSize, dotWidth)でそのまま1点分の大きさに収まる
            fallbackChevronSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
            fallbackChevronSprite.hideFlags = HideFlags.HideAndDontSave;
            return fallbackChevronSprite;
        }

        static Sprite GetFallbackArrowSprite()
        {
            if (fallbackArrowSprite != null) return fallbackArrowSprite;

            const int size = 32;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave
            };

            // 左端(底辺)から右端(頂点)へ向けて幅が線形に狭まる二等辺三角形を、アルファマスクとして焼き込む
            for (var x = 0; x < size; x++)
            {
                var u = x / (float)(size - 1);
                var halfHeight = (1f - u) * 0.5f;

                for (var y = 0; y < size; y++)
                {
                    var v = y / (float)(size - 1) - 0.5f;
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Abs(v) <= halfHeight ? 1f : 0f));
                }
            }
            texture.Apply();

            // pivot を底辺中央(左端)にして、localPosition=底辺・localScale.x=矢印の長さで頂点が +X 側に伸びるようにする
            fallbackArrowSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0f, 0.5f), size);
            fallbackArrowSprite.hideFlags = HideFlags.HideAndDontSave;
            return fallbackArrowSprite;
        }
    }
}
