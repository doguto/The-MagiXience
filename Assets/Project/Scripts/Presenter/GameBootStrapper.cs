using Project.Scripts.Extensions;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Project.Scripts.Presenter
{
    public class GameBootStrapper : MonoBehaviour
    {
        #if UNITY_EDITOR
        // Entryシーンを経由しない開発時(各シーン単体プレイなど)のフォールバック。
        // Entryシーン経由ならEntryScenePresenterがGlobalをロードするのでこの処理はスキップされる。
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void EnsureGlobalSceneLoaded()
        {
            var globalLoaded = false;
            var globalSceneName = SceneType.Global.ToSceneName();
            var entrySceneName = SceneType.Entry.ToSceneName();
        
            // 現在ロード済みシーンをチェック
            for (var i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene.name == globalSceneName)
                {
                    globalLoaded = true;
                    break;
                }
                // Entryシーン経由なら、EntryScenePresenter側で初期化するのでスキップ
                if (scene.name == entrySceneName)
                {
                    globalLoaded = true;
                    return;
                }
            }
        
            if (globalLoaded) return;
        
            Debug.Log("[GlobalSceneBootstrapper] Loading GlobalScene...");
            SceneManager.LoadScene(globalSceneName, LoadSceneMode.Additive);
        }
        #endif
    }
}
