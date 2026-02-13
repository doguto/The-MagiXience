using UnityEngine;

namespace Project.Scenes.Battle.Scripts.View.Entity
{
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(Collider2D))]
    public class BulletEntityView : MonoBehaviour
    {
        [SerializeField] SpriteRenderer spriteRenderer;
        // memo: TrailRendererというものがあるらしい

        void Awake()
        {
            spriteRenderer ??= GetComponent<SpriteRenderer>();
        }

        public void UpdatePosition(Vector3 position)
        {
            transform.position = position;
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
