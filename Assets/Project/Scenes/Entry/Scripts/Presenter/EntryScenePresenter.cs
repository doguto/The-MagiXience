using Cysharp.Threading.Tasks;
using Project.Scripts.Extensions;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;

public class EntryScenePresenter : MonoBehaviour
{
    async void Awake()
    {
        Debug.Log("[EntryScenePresenter] Awake start");

        var globalSceneName = SceneType.Global.ToSceneName();
        var titleSceneName = SceneType.Title.ToSceneName();

        Debug.Log("[EntryScenePresenter] before Addressables.InitializeAsync");
        await Addressables.InitializeAsync().ToUniTask();
        Debug.Log("[EntryScenePresenter] after Addressables.InitializeAsync");

        Debug.Log("[EntryScenePresenter] before LoadSceneAsync Global");
        await SceneManager.LoadSceneAsync(globalSceneName, LoadSceneMode.Additive).ToUniTask();
        Debug.Log("[EntryScenePresenter] after LoadSceneAsync Global");

        Debug.Log("[EntryScenePresenter] before LoadSceneAsync Title");
        await SceneManager.LoadSceneAsync(titleSceneName, LoadSceneMode.Additive).ToUniTask();
        Debug.Log("[EntryScenePresenter] after LoadSceneAsync Title");

        SceneManager.SetActiveScene(SceneManager.GetSceneByName(titleSceneName));
        Debug.Log("[EntryScenePresenter] after SetActiveScene");

        await SceneManager.UnloadSceneAsync(gameObject.scene).ToUniTask();
        Debug.Log("[EntryScenePresenter] after UnloadSceneAsync Entry");
    }
}
