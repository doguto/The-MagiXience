using UnityEngine;

namespace Project.Scenes.Battle.Scripts.View.Entity
{
    public class BulletEntityView : EntityViewBase
    {
        [SerializeField] SpriteRenderer spriteRenderer;
        // memo: TrailRendererというものがあるらしい

        void Awake()
        {
            spriteRenderer ??= GetComponent<SpriteRenderer>();
            OnAwakeView();
        }

        public void SetVisible(bool visible)
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.enabled = visible;
            }
        }

        /// ObjectPool用
        public void ResetView()
        {
            SetVisible(true);
        }
    }
}
