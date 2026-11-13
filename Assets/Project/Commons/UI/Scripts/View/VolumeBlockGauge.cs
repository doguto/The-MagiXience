using UnityEngine;
using UnityEngine.UI;

namespace Project.Commons.UI.Scripts.View
{
    // 0-100の値をブロック列で表示する。端数のブロックは固定角度の斜線で塗る
    [RequireComponent(typeof(CanvasRenderer))]
    public class VolumeBlockGauge : MaskableGraphic
    {
        [SerializeField, Range(0, 100)] int value = 70;
        [SerializeField, Min(1)] int blockCount = 10;
        [SerializeField, Min(0f)] float blockSpacing = 10f;
        [Tooltip("斜線の横幅(ブロック幅比)。0で垂直、1で45度")]
        [SerializeField, Range(0f, 1f)] float slant = 0.35f;
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
                AddFill(vh, x0, y0, size, fill, slant, Color.Lerp(lowColor, highColor, t) * color);
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

        static readonly Vector2[] Corners = new Vector2[4];

        // 境界線は左上→右下の固定角度で、ブロックの左外から右外へ等速でスライドする
        static void AddFill(VertexHelper vh, float x0, float y0, float size, float fill, float slant, Color32 c)
        {
            if (fill >= 1f)
            {
                AddQuad(vh, x0, y0, size, c);
                return;
            }

            var x1 = x0 + size;
            var y1 = y0 + size;
            var run = slant * size;
            var topX = x0 - run + fill * (size + run);

            Corners[0] = new Vector2(x0, y0);
            Corners[1] = new Vector2(x0, y1);
            Corners[2] = new Vector2(x1, y1);
            Corners[3] = new Vector2(x1, y0);

            var start = vh.currentVertCount;
            var count = 0;
            for (var i = 0; i < 4; i++)
            {
                var p = Corners[i];
                var q = Corners[(i + 1) % 4];
                var fp = Side(p, topX, run, y1, size);
                var fq = Side(q, topX, run, y1, size);
                if (fp >= 0f)
                {
                    vh.AddVert(p, c, Vector2.zero);
                    count++;
                }
                if (fp >= 0f != fq >= 0f)
                {
                    vh.AddVert(Vector2.Lerp(p, q, fp / (fp - fq)), c, Vector2.zero);
                    count++;
                }
            }

            for (var i = 1; i < count - 1; i++)
                vh.AddTriangle(start, start + i, start + i + 1);
        }

        static float Side(Vector2 p, float topX, float run, float y1, float size) =>
            topX + run * (y1 - p.y) / size - p.x;

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            SetVerticesDirty();
        }
#endif
    }
}
