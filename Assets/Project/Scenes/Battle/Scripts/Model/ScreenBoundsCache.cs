using UnityEngine;
using UnityEngine.SceneManagement;

namespace Project.Scenes.Battle.Scripts.Model
{
    public static class ScreenBoundsCache
    {
        public static float MinX { get; private set; }
        public static float MaxX { get; private set; }
        public static float MinY { get; private set; }
        public static float MaxY { get; private set; }

        public static void Initialize(Camera camera)
        {
            var bottomLeft = camera.ViewportToWorldPoint(Vector3.zero);
            var topRight = camera.ViewportToWorldPoint(Vector3.one);
            MinX = bottomLeft.x;
            MaxX = topRight.x;
            MinY = bottomLeft.y;
            MaxY = topRight.y;
        }

        public static void Initialize(Scene scene)
        {
            foreach (var cam in Camera.allCameras)
            {
                if (cam.gameObject.scene == scene)
                {
                    Initialize(cam);
                    return;
                }
            }
            Debug.LogWarning("[ScreenBoundsCache] No camera found in scene: " + scene.name);
        }
    }
}
