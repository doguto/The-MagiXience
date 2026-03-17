using Cysharp.Threading.Tasks;
using Project.Scenes.DemoClear.Scripts.View;
using Project.Scripts.Model;
using Project.Scripts.Presenter;
using UniRx;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Project.Scenes.DemoClear.Scripts.Presenter
{
    public class DemoClearScenePresenter : MonoPresenter
    {
        [SerializeField] DemoClearView demoClearView;

        protected override void Start()
        {
            base.Start();

            
            demoClearView.OnAnyKeyPressed
                .Take(1)
                .Subscribe(_ =>
                    
                    LoadTitle().Forget())
                .AddTo(this);

            demoClearView.Show();
        }

        async UniTask LoadTitle()
        {
            await SceneManager.LoadSceneAsync(SceneRouterModel.Title, LoadSceneMode.Additive).ToUniTask();
            await SceneManager.UnloadSceneAsync(SceneRouterModel.Battle).ToUniTask();
            await SceneManager.UnloadSceneAsync(gameObject.scene.name).ToUniTask();
            
            SceneManager.SetActiveScene(SceneManager.GetSceneByName(SceneRouterModel.Title));
        }
    }
}
