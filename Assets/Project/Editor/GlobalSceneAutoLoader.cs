using Project.Scripts.Extensions;
using UnityEditor;
using UnityEditor.SceneManagement;
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
        }
    
        static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.ExitingEditMode) return;
            EnsureGlobalSceneLoaded();
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
