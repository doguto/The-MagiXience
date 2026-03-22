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
    }
}
