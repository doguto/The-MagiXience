using Cysharp.Threading.Tasks;
using Project.Scripts.Extensions.Message;
using UniRx;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Project.Scenes.Global.Scripts.Presenter
{
    public class SceneNavigator
    {
        // 遷移中か。別々の入力 (決定とキャンセルなど) から遷移が二重に走らないよう、遷移中の NavigateTo は無視する
        public bool IsNavigating { get; private set; }

        public async UniTask NavigateTo(string toSceneName, string fromSceneName)
        {
            if (IsNavigating)
            {
                Debug.LogWarning($"[SceneNavigator] Ignored Scene Navigation while navigating: from {fromSceneName} to {toSceneName}");
                return;
            }
            IsNavigating = true;

            try
            {
                Debug.Log($"[SceneNavigator] Start Scene Navigation: from {fromSceneName} to {toSceneName}");
                MessageBroker.Default.Publish(new SceneNavigationMessage(SceneNavigationState.InProgress, toSceneName));

                await SceneManager.LoadSceneAsync(toSceneName, LoadSceneMode.Additive).ToUniTask();

                SceneManager.SetActiveScene(SceneManager.GetSceneByName(toSceneName));
                var unloadTask = SceneManager.UnloadSceneAsync(fromSceneName).ToUniTask();

                Debug.Log($"[SceneNavigator] Compele Scene Navigation: from {fromSceneName} to {toSceneName}");
                MessageBroker.Default.Publish(new SceneNavigationMessage(SceneNavigationState.Completed, toSceneName));

                // 破棄中の遷移元シーンが入力を受けて遷移し直さないよう、アンロードが終わるまで遷移中として扱う
                await unloadTask;
            }
            finally
            {
                IsNavigating = false;
            }
        }
    }
}
