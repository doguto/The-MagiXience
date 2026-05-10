using UnityEngine;
using UnityEngine.UI;

namespace Project.Scenes.Battle.Scripts.View.Entity
{
    public class PlayerEntityView : EntityViewBase
    {
        [Header("Damage Flash")]
        [SerializeField] SpriteRenderer bodySpriteRenderer;
        [SerializeField] Color flashColor = Color.black;

        // memo: HpBarを大量に場に出すことになったら、Canvasを使わない設計にしたい
        // Claudeの提案；SpriteRenderer + Material Property （カスタムシェーダー）、9-Sliced Sprite + Size 直接操作、Quad Mesh自作 (最軽量だがやや手間)
        [Header("Hp Bar")]
        [SerializeField] Image hpBarFillImage;

        Color originalBodyColor;

        void Reset()
        {
            bodySpriteRenderer = GetComponent<SpriteRenderer>();
        }

        protected override void OnAwakeView()
        {
            if (bodySpriteRenderer == null)
            {
                bodySpriteRenderer = GetComponent<SpriteRenderer>();
            }
            if (bodySpriteRenderer != null)
            {
                originalBodyColor = bodySpriteRenderer.color;
            }
        }

        public void SetDamageFlashActive(bool active)
        {
            if (bodySpriteRenderer == null) return;
            bodySpriteRenderer.color = active ? flashColor : originalBodyColor;
        }

        public void ResetDamageFlash()
        {
            if (bodySpriteRenderer == null) return;
            bodySpriteRenderer.color = originalBodyColor;
        }

        public void SetHpRatio(float ratio)
        {
            if (hpBarFillImage == null) return;
            hpBarFillImage.fillAmount = Mathf.Clamp01(ratio);
        }
    }
}
