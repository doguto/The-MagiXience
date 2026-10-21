using Cysharp.Threading.Tasks;
using Project.Scenes.DemoClear.Scripts.View;
using Project.Scripts.Extensions;
using Project.Scripts.Model;
using Project.Scripts.Presenter;
using Project.Scripts.Repository.ModelRepository;
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
                .Subscribe(_ => LoadStageList().Forget())
                .AddTo(this);

            demoClearView.Show();
        }

        async UniTask LoadStageList()
        {
            var titleBgmType = UserModelRepository.Instance.Get().TitleBgmType;
            soundManager?.PlayBGMAsync(SceneType.Title, titleBgmType).Forget();

            await SceneManager.LoadSceneAsync(SceneRouterModel.StageList, LoadSceneMode.Additive).ToUniTask();
            SceneManager.SetActiveScene(SceneManager.GetSceneByName(SceneRouterModel.StageList));

            await SceneManager.UnloadSceneAsync(SceneRouterModel.Battle).ToUniTask();
            await SceneManager.UnloadSceneAsync(gameObject.scene.name).ToUniTask();
        }
    }
}
