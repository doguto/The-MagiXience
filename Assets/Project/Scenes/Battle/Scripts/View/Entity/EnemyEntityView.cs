using UnityEngine;

namespace Project.Scenes.Battle.Scripts.View.Entity
{
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(Collider2D))]
    public class EnemyEntityView : MonoBehaviour
    {
        public void UpdatePosition(Vector3 position)
        {
            transform.position = position;
        }
    }
}
