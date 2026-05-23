using Cysharp.Threading.Tasks;
using Project.Scripts.Extensions;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;

public class EntryScenePresenter : MonoBehaviour
{
    async void Awake()
    {

        var globalSceneName = SceneType.Global.ToSceneName();
        var titleSceneName = SceneType.Title.ToSceneName();

        await Addressables.InitializeAsync().ToUniTask();

        await SceneManager.LoadSceneAsync(globalSceneName, LoadSceneMode.Additive).ToUniTask();
        await SceneManager.LoadSceneAsync(titleSceneName, LoadSceneMode.Additive).ToUniTask();

        SceneManager.SetActiveScene(SceneManager.GetSceneByName(titleSceneName));
        await SceneManager.UnloadSceneAsync(gameObject.scene).ToUniTask();
    }
}
