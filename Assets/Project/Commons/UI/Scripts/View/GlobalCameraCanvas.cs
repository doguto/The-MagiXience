using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Project.Commons.UI.Scripts.View
{
    // Globalシーンのカメラを、別シーンのCanvasにも割り当てる。
    // Edit中もGame Viewを正しく表示するため、ExecuteAlwaysで登録する(カメラはEditorスクリプトから設定する)。
    [ExecuteAlways]
    [RequireComponent(typeof(Canvas))]
    public class GlobalCameraCanvas : MonoBehaviour
    {
        static readonly List<GlobalCameraCanvas> activeCanvases = new();
        static Camera globalCamera;

        Canvas canvas;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            activeCanvases.Clear();
            globalCamera = null;
        }

        public static void SetCamera(Camera camera)
        {
            globalCamera = camera;
            foreach (var target in activeCanvases)
            {
                if (target != null) target.Apply(globalCamera);
            }
        }

        public static void SetCameraInScene(Scene scene, Camera camera)
        {
            foreach (var target in activeCanvases)
            {
                if (target != null && target.gameObject.scene == scene) target.Apply(camera);
            }
        }

        void OnEnable()
        {
#if UNITY_EDITOR
            // Prefab編集中のCanvasにシーンのカメラを入れると、Prefabに保存できない参照が混ざる
            if (UnityEditor.SceneManagement.EditorSceneManager.IsPreviewScene(gameObject.scene)) return;
#endif
            if (!activeCanvases.Contains(this)) activeCanvases.Add(this);
            if (globalCamera != null) Apply(globalCamera);
        }

        void OnDisable()
        {
            activeCanvases.Remove(this);
        }

        void Apply(Camera camera)
        {
            if (canvas == null) canvas = GetComponent<Canvas>();
            if (canvas.worldCamera != camera) canvas.worldCamera = camera;
        }
    }
}
