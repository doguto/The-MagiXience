using UnityEngine;
using UnityEngine.UI;

namespace Project.Scenes.Battle.Scripts.View
{
    /// <summary>
    /// 風向きの矢印グリッドを描く帯1本分のView。RawImageのテクスチャに下向きの矢印1つ分を設定し、
    /// WindArrowsシェーダーで敷き詰める。向きとスクロール量はPresenterから受け取る。
    /// 背景の子に置き、背景1ドット = pixelsPerUnit分の1 となるRect単位で配置する。
    /// </summary>
    [RequireComponent(typeof(RawImage))]
    public class WindArrowsView : MonoBehaviour
    {
        static readonly int RectPixelSizeId = Shader.PropertyToID("_RectPixelSize");
        static readonly int CellSizeId = Shader.PropertyToID("_CellSize");
        static readonly int ArrowScaleId = Shader.PropertyToID("_ArrowScale");
        static readonly int OffsetId = Shader.PropertyToID("_Offset");
        static readonly int DirectionId = Shader.PropertyToID("_Direction");
        static readonly int AlphaTopId = Shader.PropertyToID("_AlphaTop");
        static readonly int AlphaBottomId = Shader.PropertyToID("_AlphaBottom");
        static readonly int AlphaStepsId = Shader.PropertyToID("_AlphaSteps");
        static readonly int PixelSnapId = Shader.PropertyToID("_PixelSnap");

        [SerializeField] Shader shader;
        [SerializeField, Min(1f), Tooltip("Rect 1単位あたりの背景ドット数。背景の子なら100。")]
        float pixelsPerUnit = 100f;
        [SerializeField, Tooltip("矢印1つ分のセルの大きさ(背景ドット)。")]
        Vector2 cellSize = new(12f, 12f);
        [SerializeField, Min(0.01f), Tooltip("矢印テクスチャ1テクセルあたりの背景ドット数。")]
        float arrowScale = 0.5f;
        [SerializeField, Range(0f, 1f)] float alphaTop = 0.6f;
        [SerializeField, Range(0f, 1f)] float alphaBottom = 0.1f;
        [SerializeField, Min(0), Tooltip("縦の透明度の段階数。0でなめらかに変化。")]
        int alphaSteps;
        [SerializeField] bool pixelSnap = true;

        RawImage image;
        Material material;

        void Awake()
        {
            image = GetComponent<RawImage>();
            image.raycastTarget = false;
            material = new Material(shader);
            image.material = material;
            ApplySettings();
        }

        void OnValidate()
        {
            if (material != null) ApplySettings();
        }

        void OnRectTransformDimensionsChange()
        {
            if (material != null) ApplySettings();
        }

        void ApplySettings()
        {
            material.SetVector(RectPixelSizeId, ((RectTransform)transform).rect.size * pixelsPerUnit);
            material.SetVector(CellSizeId, cellSize);
            material.SetFloat(ArrowScaleId, arrowScale);
            material.SetFloat(AlphaTopId, alphaTop);
            material.SetFloat(AlphaBottomId, alphaBottom);
            material.SetFloat(AlphaStepsId, alphaSteps);
            material.SetFloat(PixelSnapId, pixelSnap ? 1f : 0f);
        }

        public void SetVisible(bool visible)
        {
            if (image != null && image.enabled != visible) image.enabled = visible;
        }

        public void SetDirection(Vector2 direction)
        {
            if (material != null) material.SetVector(DirectionId, direction);
        }

        /// <summary>矢印をずらす量(背景ドット)。セル単位で繰り返すので桁が増えないよう畳んで渡す。</summary>
        public void ApplyOffset(Vector2 offset)
        {
            if (material == null) return;
            material.SetVector(OffsetId, new Vector2(
                Mathf.Repeat(offset.x, Mathf.Max(1f, cellSize.x)),
                Mathf.Repeat(offset.y, Mathf.Max(1f, cellSize.y))));
        }

        void OnDestroy()
        {
            if (material != null) Destroy(material);
        }
    }
}
