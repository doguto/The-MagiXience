using UnityEngine;

namespace Project.Scenes.Battle.Scripts.View
{
    [RequireComponent(typeof(PolygonCollider2D))]
    public class FanCollider2D : MonoBehaviour
    {
        [SerializeField, Tooltip("扇形の半径")]
        float radius = 1f;

        [SerializeField, Range(1f, 360f), Tooltip("扇形の角度（度）")]
        float angle = 90f;

        [SerializeField, Range(0f, 360f), Tooltip("正面方向のオフセット（度）。0=上、90=右、180=下、270=左")]
        float directionOffset;

        [SerializeField, Range(3, 30), Tooltip("弧の分割数")]
        int segments = 10;

        PolygonCollider2D polygonCollider;

        void Awake()
        {
            polygonCollider = GetComponent<PolygonCollider2D>();
            polygonCollider.isTrigger = true;
            UpdateCollider();
        }

        void UpdateCollider()
        {
            var points = new Vector2[segments + 2];
            points[0] = Vector2.zero;

            for (var i = 0; i <= segments; i++)
            {
                var deg = -angle / 2 + angle * i / segments + directionOffset;
                var rad = Mathf.Deg2Rad * deg;
                points[i + 1] = new Vector2(Mathf.Sin(rad), Mathf.Cos(rad)) * radius;
            }

            polygonCollider.SetPath(0, points);
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            if (polygonCollider == null)
                polygonCollider = GetComponent<PolygonCollider2D>();

            if (polygonCollider != null)
                UpdateCollider();
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
            Gizmos.matrix = transform.localToWorldMatrix;

            for (var i = 0; i < segments; i++)
            {
                var rad1 = Mathf.Deg2Rad * (-angle / 2 + angle * i / segments + directionOffset);
                var rad2 = Mathf.Deg2Rad * (-angle / 2 + angle * (i + 1) / segments + directionOffset);

                var p1 = new Vector3(Mathf.Sin(rad1), Mathf.Cos(rad1)) * radius;
                var p2 = new Vector3(Mathf.Sin(rad2), Mathf.Cos(rad2)) * radius;

                Gizmos.DrawLine(Vector3.zero, p1);
                Gizmos.DrawLine(p1, p2);
            }

            var lastRad = Mathf.Deg2Rad * (angle / 2 + directionOffset);
            Gizmos.DrawLine(Vector3.zero, new Vector3(Mathf.Sin(lastRad), Mathf.Cos(lastRad)) * radius);
        }
#endif
    }
}
