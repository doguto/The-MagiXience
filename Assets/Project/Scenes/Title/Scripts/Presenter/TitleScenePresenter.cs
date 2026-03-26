using Cysharp.Threading.Tasks;
using Project.Scripts.Model;
using Project.Scenes.Title.Scripts.Model;
using Project.Scenes.Title.Scripts.Repository.ModelRepository;
using Project.Scenes.Title.Scripts.View;
using Project.Scripts.Extensions;
using Project.Scripts.Presenter;
using UniRx;
using UnityEngine;

namespace Project.Scenes.Title.Scripts.Presenter
{
    public class TitleScenePresenter : MonoPresenter
    {
        [SerializeField] TitleMenuView titleMenuView;

        TitleModelRepository titleModelRepository;
        TitleModel titleModel;


        void Awake()
        {
            titleModelRepository = TitleModelRepository.Instance;
            titleModel = titleModelRepository.Get();

            titleMenuView.Init(titleModel.GetBackGroundSprites());
        }

        protected override void Start()
        {
            base.Start();
            titleMenuView.InitStart();

            titleMenuView.OnPressedStart.Subscribe(x =>
            {
                soundManager.PlaySEAsync(SeType.Click).Forget();
                StartGame(x).Forget();
            });
            titleMenuView.OnPressedOption.Subscribe(async _ =>
            {
                soundManager.PlaySEAsync(SeType.Click).Forget();
                titleMenuView.SetInteractable(false);
                GlobalScenePresenter.OptionModalPresenter.Open();

                await GlobalScenePresenter.OptionModalPresenter.OnClosed.ToUniTask(true);

                titleMenuView.SetInteractable(true);
                titleMenuView.InitStart();
            });

            titleMenuView.OnPressedExit.Subscribe(_ =>
            {
                soundManager.PlaySE(SeType.Cancel);
                ExitGame();
            });

            soundManager!.PlayBGMAsync(SceneType.Title).Forget();
        }

        async UniTask StartGame(Unit _)
        {
            // TitleScene 以外で TitleModel は使用しないのでクリアする
            titleModelRepository.Refresh();

            await SceneNavigator.NavigateTo(SceneRouterModel.StageList, gameObject.scene.name);
        }

        void ExitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit(); //ゲームプレイ終了
#endif
        }
    }
}
