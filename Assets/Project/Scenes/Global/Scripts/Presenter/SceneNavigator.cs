using Cysharp.Threading.Tasks;
using Project.Scripts.Extensions.Message;
using UniRx;
using UnityEngine.SceneManagement;

namespace Project.Scenes.Global.Scripts.Presenter
{
    public class SceneNavigator
    {
        public async UniTask NavigateTo(string toSceneName, string fromSceneName)
        {
            MessageBroker.Default.Publish(new SceneNavigationMessage(SceneNavigationState.InProgress, toSceneName));

            await SceneManager.LoadSceneAsync(toSceneName, LoadSceneMode.Additive).ToUniTask();

            SceneManager.SetActiveScene(SceneManager.GetSceneByName(toSceneName));
            SceneManager.UnloadSceneAsync(fromSceneName).ToUniTask().Forget();

            MessageBroker.Default.Publish(new SceneNavigationMessage(SceneNavigationState.Completed, toSceneName));
        }
    }
}
