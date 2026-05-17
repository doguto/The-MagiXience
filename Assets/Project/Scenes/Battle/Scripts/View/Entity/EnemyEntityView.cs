using UnityEngine;

namespace Project.Scenes.Battle.Scripts.View.Entity
{
    public class EnemyEntityView : EntityViewBase
    {
        [Header("Damage Flash")]
        [SerializeField] SpriteRenderer bodySpriteRenderer;
        [SerializeField] Color flashColor = Color.black;

        Color originalBodyColor;
        bool originalColorCaptured;

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
                originalColorCaptured = true;
            }
        }

        public void SetDamageFlashActive(bool active)
        {
            if (bodySpriteRenderer == null) return;
            if (!originalColorCaptured)
            {
                originalBodyColor = bodySpriteRenderer.color;
                originalColorCaptured = true;
            }
            bodySpriteRenderer.color = active ? flashColor : originalBodyColor;
        }

        public void ResetDamageFlash()
        {
            if (bodySpriteRenderer == null) return;
            if (!originalColorCaptured) return;
            bodySpriteRenderer.color = originalBodyColor;
        }
    }
}
