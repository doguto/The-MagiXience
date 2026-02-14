using UnityEngine;

namespace Project.Scenes.Battle.Scripts.View.Entity
{
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(Collider2D))]
    public abstract class EntityViewBase : MonoBehaviour
    {
        public void UpdatePosition(Vector3 position)
        {
            transform.position = position;
        }

        public Vector3 GetPosition()
        {
            return transform.position;
        }

        protected virtual void OnDestroyView()
        {
            // サブクラスでオーバーライド可能
        }

        protected virtual void OnAwakeView()
        {
            // サブクラスでオーバーライド可能
        }

        protected void DestroyGameObject()
        {
            Destroy(gameObject);
        }
    }
}
