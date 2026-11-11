using Cysharp.Threading.Tasks;
using Project.Scripts.Extensions.Message;
using UniRx;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Project.Scenes.Global.Scripts.Presenter
{
    public class SceneNavigator
    {
        public async UniTask NavigateTo(string toSceneName, string fromSceneName)
        {
            Debug.Log($"[SceneNavigator] Start Scene Navigation: from {fromSceneName} to {toSceneName}");
            MessageBroker.Default.Publish(new SceneNavigationMessage(SceneNavigationState.InProgress, toSceneName));

            await SceneManager.LoadSceneAsync(toSceneName, LoadSceneMode.Additive).ToUniTask();

            SceneManager.SetActiveScene(SceneManager.GetSceneByName(toSceneName));
            SceneManager.UnloadSceneAsync(fromSceneName).ToUniTask().Forget();

            Debug.Log($"[SceneNavigator] Compele Scene Navigation: from {fromSceneName} to {toSceneName}");
            MessageBroker.Default.Publish(new SceneNavigationMessage(SceneNavigationState.Completed, toSceneName));
        }

        // 同じシーンを読み込み直す。同名シーンが2つ同時に存在しないよう、先にアンロードしてからロードする。
        public async UniTask Reload(string sceneName)
        {
            Debug.Log($"[SceneNavigator] Start Scene Reload: {sceneName}");
            MessageBroker.Default.Publish(new SceneNavigationMessage(SceneNavigationState.InProgress, sceneName));

            await SceneManager.UnloadSceneAsync(sceneName).ToUniTask();
            await SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive).ToUniTask();

            SceneManager.SetActiveScene(SceneManager.GetSceneByName(sceneName));

            Debug.Log($"[SceneNavigator] Complete Scene Reload: {sceneName}");
            MessageBroker.Default.Publish(new SceneNavigationMessage(SceneNavigationState.Completed, sceneName));
        }
    }
}
