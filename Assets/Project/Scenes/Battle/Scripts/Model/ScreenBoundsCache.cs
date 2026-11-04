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

        // 描画領域のアスペクト比。GlobalScenePresenterがカメラのViewport Rectで16:9に固定している。
        const float TargetAspect = 16f / 9f;

        public static void Initialize(Camera camera)
        {
            // シーンのAwake時点では、GlobalScenePresenterによるViewport Rectの補正がまだ新しいカメラに適用されておらず、
            // camera.aspectがウィンドウのアスペクト比のままになっている。ウィンドウが16:9でないと画面端が実際の描画領域より
            // 広く計算され、プレイヤーなどが画面外へはみ出してしまうため、16:9として計算する。
            camera.aspect = TargetAspect;
            var bottomLeft = camera.ViewportToWorldPoint(Vector3.zero);
            var topRight = camera.ViewportToWorldPoint(Vector3.one);
            camera.ResetAspect();
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
