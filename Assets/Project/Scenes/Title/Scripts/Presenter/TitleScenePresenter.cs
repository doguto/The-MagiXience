using System;
using Cysharp.Threading.Tasks;
using Project.Commons.UI.Scripts.Presenter;
using Project.Scripts.Model;
using Project.Scenes.Title.Scripts.Model;
using Project.Scenes.Title.Scripts.Repository.ModelRepository;
using Project.Scenes.Title.Scripts.View;
using Project.Scripts.Presenter;
using UniRx;
using UnityEngine;
using UnityEngine.SceneManagement;

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

        void Start()
        {
            base.Start();
            titleMenuView.InitStart();

            titleMenuView.OnPressedStart.Subscribe(x => StartGame(x).Forget());
            titleMenuView.OnPressedOption.Subscribe(async _ =>
            {
                titleMenuView.SetInteractable(false);
                globalScenePresenter.OptionModalPresenter.Open();
                
                await globalScenePresenter.OptionModalPresenter.OnClosed.ToUniTask(useFirstValue: true);
                
                titleMenuView.SetInteractable(true);
                titleMenuView.InitStart();

            });

            titleMenuView.OnPressedExit.Subscribe(ExitGame);
        }

        async UniTask StartGame(Unit _)
        {
            // TitleScene 以外で TitleModel は使用しないのでクリアする
            titleModelRepository.Refresh(); 

            await SceneManager.LoadSceneAsync(SceneRouterModel.StageList, LoadSceneMode.Additive).ToUniTask();

            SceneManager.SetActiveScene(SceneManager.GetSceneByName(SceneRouterModel.StageList));
            SceneManager.UnloadSceneAsync(gameObject.scene.name);
        }

        void ExitGame(Unit _)
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit(); //ゲームプレイ終了
#endif
        }
    }
}
