using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Project.Commons.UI.Scripts.View
{
    // 左上の角から右下の角へ向かって、斜め(右上-左下方向の境界線)に塗りつぶすUIグラフィック。
    // Image の Filled では斜め方向の塗りができないため独自に頂点を生成する。
    // fillAmount = 0 で非表示、1 で四角全体を表示し、境界線は対角線上を線形に移動する。
    public class DiagonalFillImage : MaskableGraphic
    {
        [SerializeField] Sprite sprite;
        [SerializeField, Range(0f, 1f)] float fillAmount = 1f;

        // 正規化座標(u: 左→右, w: 上→下)での四角の頂点。時計回り。
        static readonly Vector2[] QuadCorners =
        {
            new(0f, 0f), new(1f, 0f), new(1f, 1f), new(0f, 1f)
        };

        readonly List<Vector2> clipped = new(8);

        public override Texture mainTexture => sprite != null ? sprite.texture : s_WhiteTexture;

        public float FillAmount
        {
            get => fillAmount;
            set
            {
                var clamped = Mathf.Clamp01(value);
                if (Mathf.Approximately(fillAmount, clamped)) return;
                fillAmount = clamped;
                SetVerticesDirty();
            }
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (fillAmount <= 0f) return;

            // u + w <= 2 * fillAmount の領域を残す(Sutherland–Hodgman で四角をクリップ)。
            var limit = fillAmount * 2f;
            clipped.Clear();
            for (var i = 0; i < QuadCorners.Length; i++)
            {
                var a = QuadCorners[i];
                var b = QuadCorners[(i + 1) % QuadCorners.Length];
                var da = a.x + a.y - limit;
                var db = b.x + b.y - limit;
                var aInside = da <= 0f;
                var bInside = db <= 0f;

                if (aInside) clipped.Add(a);
                if (aInside != bInside) clipped.Add(Vector2.Lerp(a, b, da / (da - db)));
            }
            if (clipped.Count < 3) return;

            var rect = GetPixelAdjustedRect();
            var uv = sprite != null
                ? UnityEngine.Sprites.DataUtility.GetOuterUV(sprite)
                : new Vector4(0f, 0f, 1f, 1f);
            Color32 vertexColor = color;

            foreach (var p in clipped)
            {
                var pos = new Vector2(rect.xMin + p.x * rect.width, rect.yMax - p.y * rect.height);
                var texCoord = new Vector2(Mathf.Lerp(uv.x, uv.z, p.x), Mathf.Lerp(uv.w, uv.y, p.y));
                vh.AddVert(pos, vertexColor, texCoord);
            }
            for (var i = 1; i < clipped.Count - 1; i++)
            {
                vh.AddTriangle(0, i, i + 1);
            }
        }
    }
}
