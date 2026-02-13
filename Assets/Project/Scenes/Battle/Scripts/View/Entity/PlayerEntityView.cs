using UnityEngine;
using System.Collections;

namespace Project.Scenes.Battle.Scripts.View.Entity
{
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(Collider2D))]
    public class PlayerEntityView : MonoBehaviour
    {
        [SerializeField] SpriteRenderer spriteRenderer;

        Color originalColor;

        void Awake()
        {
            spriteRenderer ??= GetComponent<SpriteRenderer>();
            originalColor = spriteRenderer != null ? spriteRenderer.color : Color.white;
        }

        public void UpdatePosition(Vector3 position)
        {
            transform.position = position;
        }
    }
}
