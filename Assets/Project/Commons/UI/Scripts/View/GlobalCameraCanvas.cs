using System.Collections.Generic;
using UnityEngine;

namespace Project.Commons.UI.Scripts.View
{
    // Globalシーンのカメラを、別シーンのCanvasにも割り当てる。
    [RequireComponent(typeof(Canvas))]
    public class GlobalCameraCanvas : MonoBehaviour
    {
        static readonly List<GlobalCameraCanvas> pendingCanvases = new();
        static Camera globalCamera;

        Canvas canvas;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            pendingCanvases.Clear();
            globalCamera = null;
        }

        public static void SetCamera(Camera camera)
        {
            globalCamera = camera;
            if (globalCamera == null) return;

            foreach (var pending in pendingCanvases)
            {
                if (pending != null) pending.Apply();
            }
            pendingCanvases.Clear();
        }

        void Awake()
        {
            canvas = GetComponent<Canvas>();
        }

        void OnEnable()
        {
            if (globalCamera != null)
            {
                Apply();
            }
            else if (!pendingCanvases.Contains(this))
            {
                pendingCanvases.Add(this);
            }
        }

        void OnDisable()
        {
            pendingCanvases.Remove(this);
        }

        void Apply()
        {
            canvas.worldCamera = globalCamera;
        }
    }
}
