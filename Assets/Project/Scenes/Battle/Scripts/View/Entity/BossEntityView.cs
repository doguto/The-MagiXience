using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Project.Scenes.Battle.Scripts.View.Entity
{
    public class BossEntityView : EnemyEntityView
    {
        // memo: 色帯は「満タンの固定絵」として bandsContainer 上に並べ、その全体を
        //       hpBarMask (RectMask2D) で右から削る。色帯自体は動かさず、マスク幅だけを
        //       残量割合に応じて変える。区切りはツリーの HpSections から受け取る。
        [Header("Hp Bar")]
        [Tooltip("残量割合に応じて幅を変えるマスク。この子に色帯が並ぶ")]
        [SerializeField] RectMask2D hpBarMask;
        [Tooltip("色帯を並べる全幅固定のコンテナ。hpBarMask の子")]
        [SerializeField] RectTransform bandsContainer;
        [Tooltip("色帯に使うスプライト（UiFlatSquare など単色ベタ）")]
        [SerializeField] Sprite bandSprite;
        [Tooltip("区間の色。満タン側の区間が先頭、最後の区間が末尾。区間数に合わせて間を拾う")]
        [SerializeField] Color[] sectionColors =
        {
            new(0.81f, 0.85f, 0.10f), // 黄
            new(0.90f, 0.50f, 0.10f), // 橙
            new(0.85f, 0.10f, 0.10f), // 赤
        };

        readonly List<GameObject> bands = new();
        RectTransform maskRect;
        float fullWidth;

        protected override void OnAwakeView()
        {
            base.OnAwakeView();
            if (hpBarMask != null) maskRect = hpBarMask.rectTransform;
            SetHpBreaks(System.Array.Empty<float>());
        }

        /// <summary>
        /// 区切り (0〜1、大きい順) から色帯を作り直す。区間 i は [区切り i, 区切り i-1] の範囲。
        /// </summary>
        public void SetHpBreaks(IReadOnlyList<float> breaks)
        {
            if (bandsContainer == null || sectionColors == null || sectionColors.Length == 0) return;

            foreach (var band in bands) Destroy(band);
            bands.Clear();

            var count = breaks.Count + 1;
            var upper = 1f;
            for (var i = 0; i < count; i++)
            {
                var lower = i < breaks.Count ? Mathf.Clamp01(breaks[i]) : 0f;
                if (upper > lower) CreateBand(lower, upper, SectionColor(i, count), i);
                upper = lower;
            }
        }

        Color SectionColor(int index, int count)
        {
            if (count <= 1) return sectionColors[0];
            var at = Mathf.RoundToInt(index * (sectionColors.Length - 1) / (float)(count - 1));
            return sectionColors[Mathf.Clamp(at, 0, sectionColors.Length - 1)];
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
            bands.Add(go);
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
