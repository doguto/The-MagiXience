using UnityEngine;
using UnityEngine.UI;

namespace Project.Commons.UI.Scripts.View
{
    // 0-100の値をブロック列で表示する。端数のブロックは右上から斜めに削る
    [RequireComponent(typeof(CanvasRenderer))]
    public class VolumeBlockGauge : MaskableGraphic
    {
        [SerializeField, Range(0, 100)] int value = 70;
        [SerializeField, Min(1)] int blockCount = 10;
        [SerializeField, Min(0f)] float blockSpacing = 10f;
        [SerializeField] Color emptyColor = new(0.14f, 0.06f, 0.05f, 1f);
        [SerializeField] Color lowColor = new Color32(56, 56, 56, 255);
        [SerializeField] Color highColor = new Color32(228, 228, 228, 255);

        public int Value
        {
            get => value;
            set
            {
                var clamped = Mathf.Clamp(value, 0, 100);
                if (clamped == this.value) return;
                this.value = clamped;
                SetVerticesDirty();
            }
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();

            var rect = GetPixelAdjustedRect();
            var size = Mathf.Min(rect.height, (rect.width - blockSpacing * (blockCount - 1)) / blockCount);
            if (size <= 0f) return;

            var valuePerBlock = 100f / blockCount;
            var y0 = rect.center.y - size * 0.5f;

            for (var i = 0; i < blockCount; i++)
            {
                var x0 = rect.xMin + i * (size + blockSpacing);
                AddQuad(vh, x0, y0, size, emptyColor);

                var fill = Mathf.Clamp01((value - i * valuePerBlock) / valuePerBlock);
                if (fill <= 0f) continue;

                var t = blockCount == 1 ? 1f : i / (blockCount - 1f);
                AddFill(vh, x0, y0, size, fill, Color.Lerp(lowColor, highColor, t) * color);
            }
        }

        static void AddQuad(VertexHelper vh, float x0, float y0, float size, Color32 c)
        {
            var start = vh.currentVertCount;
            vh.AddVert(new Vector3(x0, y0), c, Vector2.zero);
            vh.AddVert(new Vector3(x0, y0 + size), c, Vector2.zero);
            vh.AddVert(new Vector3(x0 + size, y0 + size), c, Vector2.zero);
            vh.AddVert(new Vector3(x0 + size, y0), c, Vector2.zero);
            vh.AddTriangle(start, start + 1, start + 2);
            vh.AddTriangle(start, start + 2, start + 3);
        }

        // fill>=0.5: 右上の三角を削る / fill<0.5: 左下の三角のみ残す。面積がfillに比例する
        static void AddFill(VertexHelper vh, float x0, float y0, float size, float fill, Color32 c)
        {
            if (fill >= 1f)
            {
                AddQuad(vh, x0, y0, size, c);
                return;
            }

            var x1 = x0 + size;
            var y1 = y0 + size;
            var start = vh.currentVertCount;

            if (fill >= 0.5f)
            {
                var topX = x0 + (2f * fill - 1f) * size;
                vh.AddVert(new Vector3(x0, y0), c, Vector2.zero);
                vh.AddVert(new Vector3(x0, y1), c, Vector2.zero);
                vh.AddVert(new Vector3(topX, y1), c, Vector2.zero);
                vh.AddVert(new Vector3(x1, y0), c, Vector2.zero);
                vh.AddTriangle(start, start + 1, start + 2);
                vh.AddTriangle(start, start + 2, start + 3);
            }
            else
            {
                var bottomX = x0 + 2f * fill * size;
                vh.AddVert(new Vector3(x0, y0), c, Vector2.zero);
                vh.AddVert(new Vector3(x0, y1), c, Vector2.zero);
                vh.AddVert(new Vector3(bottomX, y0), c, Vector2.zero);
                vh.AddTriangle(start, start + 1, start + 2);
            }
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            SetVerticesDirty();
        }
#endif
    }
}
