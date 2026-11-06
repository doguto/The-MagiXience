using Project.Commons.UI.Scripts.View;
using Project.Scenes.Global.Scripts.Presenter;
using Project.Scripts.Extensions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Project.Editor
{
    [InitializeOnLoad]
    public class GlobalSceneAutoLoader
    {
        const string GlobalScenePath = "Assets/Project/Scenes/Global.unity";

        static GlobalSceneAutoLoader()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

            // カメラはGlobalシーンにしか無いため、Edit中もGlobalを開いてGame Viewを描画できるようにする。
            EditorSceneManager.sceneOpened += OnSceneOpened;
            EditorApplication.delayCall += EnsureGlobalSceneLoadedInEditMode;

            // GlobalCameraCanvasはRuntimeでしかカメラを割り当てないため、Edit中はここで割り当てる。
            // シーンを跨ぐ参照は保存できないので、保存の間だけ外す。
            EditorApplication.hierarchyChanged += AssignCameraToCanvasesInEditMode;
            EditorSceneManager.sceneSaving += (scene, _) => SetCanvasCamera(scene, null);
            EditorSceneManager.sceneSaved += _ => AssignCameraToCanvasesInEditMode();
        }

        static void AssignCameraToCanvasesInEditMode()
        {
            if (Application.isBatchMode) return;
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;

            var camera = FindGlobalCamera();
            if (camera == null) return;

            for (var i = 0; i < SceneManager.sceneCount; i++)
            {
                SetCanvasCamera(SceneManager.GetSceneAt(i), camera);
            }
        }

        static void SetCanvasCamera(Scene scene, Camera camera)
        {
            if (!scene.isLoaded) return;
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var target in root.GetComponentsInChildren<GlobalCameraCanvas>(true))
                {
                    var canvas = target.GetComponent<Canvas>();
                    if (canvas.worldCamera != camera) canvas.worldCamera = camera;
                }
            }
        }

        static Camera FindGlobalCamera()
        {
            var globalScene = SceneManager.GetSceneByName(SceneType.Global.ToSceneName());
            if (!globalScene.isLoaded) return null;

            foreach (var root in globalScene.GetRootGameObjects())
            {
                var presenter = root.GetComponentInChildren<GlobalScenePresenter>(true);
                if (presenter == null) continue;
                return new SerializedObject(presenter).FindProperty("mainCamera").objectReferenceValue as Camera;
            }
            return null;
        }

        static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.ExitingEditMode) return;
            EnsureGlobalSceneLoaded();
        }

        static void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            // コールバック中に別シーンを開くと再入するため、次フレームで処理する。
            EditorApplication.delayCall -= EnsureGlobalSceneLoadedInEditMode;
            EditorApplication.delayCall += EnsureGlobalSceneLoadedInEditMode;
        }

        static void EnsureGlobalSceneLoadedInEditMode()
        {
            if (Application.isBatchMode) return;
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            // 未保存の新規シーンには付与しない
            if (string.IsNullOrEmpty(SceneManager.GetActiveScene().path)) return;
            EnsureGlobalSceneLoaded();
            AssignCameraToCanvasesInEditMode();
        }

        static void EnsureGlobalSceneLoaded()
        {
            // 既にGlobalSceneがロードされている場合・EntrySceneから起動した場合はスキップ
            for (var i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene.name == SceneType.Global.ToSceneName()) return;
                if (scene.name == SceneType.Entry.ToSceneName()) return;
            }

            // GlobalSceneをAdditiveモードでロード
            EditorSceneManager.OpenScene(GlobalScenePath, OpenSceneMode.Additive);
        }
    }
}
