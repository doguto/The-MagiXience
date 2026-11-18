using System;
using UnityEngine;
using UnityEngine.UI;

namespace Project.Scenes.Battle.Scripts.View.Entity
{
    public class BossEntityView : EnemyEntityView
    {
        // memo: 色帯は「満タンの固定絵」として bandsContainer 上に並べ、その全体を
        //       hpBarMask (RectMask2D) で右から削る。色帯自体は動かさず、マスク幅だけを
        //       残量割合に応じて変える。境界は colorStops で自由に定義できる。
        [Header("Hp Bar")]
        [Tooltip("残量割合に応じて幅を変えるマスク。この子に色帯が並ぶ")]
        [SerializeField] RectMask2D hpBarMask;
        [Tooltip("色帯を並べる全幅固定のコンテナ。hpBarMask の子")]
        [SerializeField] RectTransform bandsContainer;
        [Tooltip("色帯に使うスプライト（UiFlatSquare など単色ベタ）")]
        [SerializeField] Sprite bandSprite;
        [Tooltip("色帯の定義。upperRatio を降順（大きい順）で並べる")]
        [SerializeField] HpBarColorStop[] colorStops =
        {
            new() { upperRatio = 1.0f, color = new Color(0.85f, 0.10f, 0.10f) }, // 赤
            new() { upperRatio = 0.6f, color = new Color(0.90f, 0.50f, 0.10f) }, // 橙
            new() { upperRatio = 0.3f, color = new Color(0.81f, 0.85f, 0.10f) }, // 黄
        };

        RectTransform maskRect;
        float fullWidth;
        bool built;

        protected override void OnAwakeView()
        {
            base.OnAwakeView();
            BuildBands();
        }

        /// <summary>
        /// colorStops から色帯 Image を生成し、bandsContainer 上に左詰めで並べる。
        /// 各帯は anchor の割合指定で配置するので、コンテナ幅が変わっても比率を保つ。
        /// </summary>
        void BuildBands()
        {
            if (built) return;
            if (hpBarMask == null || bandsContainer == null) return;

            maskRect = hpBarMask.rectTransform;
            fullWidth = maskRect.rect.width;

            // 降順ソート（Inspector 入力が前後しても安全に動くように）
            var stops = (HpBarColorStop[])colorStops.Clone();
            Array.Sort(stops, (a, b) => b.upperRatio.CompareTo(a.upperRatio));

            float lowerRatio = 0f;
            for (int i = stops.Length - 1; i >= 0; i--)
            {
                var stop = stops[i];
                float upper = Mathf.Clamp01(stop.upperRatio);
                if (upper <= lowerRatio)
                {
                    // 幅ゼロ以下の帯はスキップ
                    continue;
                }

                CreateBand(lowerRatio, upper, stop.color, i);
                lowerRatio = upper;
            }

            built = true;
        }

        void CreateBand(float leftRatio, float rightRatio, Color color, int index)
        {
            var go = new GameObject($"Band_{index}", typeof(RectTransform), typeof(Image));
            go.layer = bandsContainer.gameObject.layer;

            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(bandsContainer, false);
            rect.anchorMin = new Vector2(leftRatio, 0f);
            rect.anchorMax = new Vector2(rightRatio, 1f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var image = go.GetComponent<Image>();
            image.sprite = bandSprite;
            image.color = color;
            image.raycastTarget = false;
        }

        /// <summary>
        /// 残量割合に応じてマスク幅を更新し、右から色帯を削る。
        /// </summary>
        public void SetHpRatio(float ratio)
        {
            if (maskRect == null) return;

            // Awake 時点でレイアウト未確定だと rect.width が 0 になり得るため、
            // 初回に有効値が取れていなければここで取り直す。
            if (fullWidth <= 0f)
            {
                fullWidth = maskRect.rect.width;
                if (fullWidth <= 0f) return;
            }

            ratio = Mathf.Clamp01(ratio);
            maskRect.sizeDelta = new Vector2(fullWidth * ratio, maskRect.sizeDelta.y);
        }
    }
}
