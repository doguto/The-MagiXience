using UnityEngine;

namespace Project.Scenes.Battle.Scripts.View.Entity
{
    public class BulletEntityView : EntityViewBase
    {
        [SerializeField] SpriteRenderer spriteRenderer;
        // memo: TrailRendererというものがあるらしい

        public void SetVisible(bool visible)
        {
            spriteRenderer.enabled = visible;
        }

        /// ObjectPool用
        public void ResetView()
        {
            SetVisible(true);
        }
    }
}
