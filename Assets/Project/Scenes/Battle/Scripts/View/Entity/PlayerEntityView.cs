using UnityEngine;
using System.Collections;

namespace Project.Scenes.Battle.Scripts.View.Entity
{
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(Collider2D))]
    public class PlayerEntityView : MonoBehaviour
    {
        public void UpdatePosition(Vector3 position)
        {
            transform.position = position;
        }
        
        public Vector3 GetPosition => transform.position;
    }
}
