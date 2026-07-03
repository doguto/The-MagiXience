using UnityEngine;
using Project.Scenes.Battle.Scripts.Model;

namespace Project.Scenes.Battle.Scripts.Presenter
{
    /// <summary>
    /// 画面の上下左右に見えない壁コライダーを配置する。
    /// 物理ボールが画面外に出ないよう反射させる。
    /// </summary>
    public class BattleWallPresenter : MonoBehaviour
    {
        [SerializeField] float wallThickness = 1f;

        void Awake()
        {
            CreateWalls();
        }

        void CreateWalls()
        {
            var minX = ScreenBoundsCache.MinX;
            var maxX = ScreenBoundsCache.MaxX;
            var minY = ScreenBoundsCache.MinY;
            var maxY = ScreenBoundsCache.MaxY;

            var width = maxX - minX;
            var height = maxY - minY;
            var centerX = (minX + maxX) * 0.5f;
            var centerY = (minY + maxY) * 0.5f;

            // 上
            CreateWall("Wall_Top",
                new Vector3(centerX, maxY + wallThickness * 0.5f, 0f),
                new Vector2(width + wallThickness * 2f, wallThickness));

            // 下
            CreateWall("Wall_Bottom",
                new Vector3(centerX, minY - wallThickness * 0.5f, 0f),
                new Vector2(width + wallThickness * 2f, wallThickness));

            // 左
            CreateWall("Wall_Left",
                new Vector3(minX - wallThickness * 0.5f, centerY, 0f),
                new Vector2(wallThickness, height + wallThickness * 2f));

            // 右
            CreateWall("Wall_Right",
                new Vector3(maxX + wallThickness * 0.5f, centerY, 0f),
                new Vector2(wallThickness, height + wallThickness * 2f));
        }

        void CreateWall(string wallName, Vector3 position, Vector2 size)
        {
            var wall = new GameObject(wallName);
            wall.transform.SetParent(transform);
            wall.transform.position = position;

            var rb = wall.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Static;

            var collider = wall.AddComponent<BoxCollider2D>();
            collider.size = size;
            collider.isTrigger = false;
        }
    }
}
