using System;
using Cysharp.Threading.Tasks;
using NUnit.Framework.Constraints;
using Project.Commons.UI.Scripts.View;
using Project.Scenes.Title.Scripts.View;
using Project.Scripts.Model;
using Project.Scripts.Presenter;
using Project.Scripts.Repository.ModelRepository;
using UniRx;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Project.Commons.UI.Scripts.Presenter
{
    public class PauseModalPresenter: MonoPresenter
    {
        
        [SerializeField] PauseModalView pauseModalView;
        IDisposable pauseEvent;
        
        readonly Subject<Unit> onClosed = new();
        public IObservable<Unit> OnClosed => onClosed;
        RuntimeModel runtimeModel;
        
        
        
        void Awake()
        {
            runtimeModel = RuntimeModelRepository.Get();
        }

        void Start()
        {
            pauseModalView.InitStart();
            
            pauseModalView.OnPressedCancel.Subscribe(_ =>
            {
                gameObject.SetActive(false);
            });
            pauseModalView.OnPressedOption.Subscribe(_ =>
            {
                globalScenePresenter.OptionModalPresenter.Open();
                gameObject.SetActive(false);

                pauseEvent = globalScenePresenter.OptionModalPresenter.OnClosed.Subscribe(_ =>
                {
                    gameObject.SetActive(true);
                    pauseModalView.InitStart();
                    pauseEvent.Dispose();
                });
            });
            pauseModalView.OnPressedExit.Subscribe(x => LoadTitle(x).Forget()).AddTo(this);
        }

        
        async UniTask LoadTitle(Unit _)
        {
            var sceneName = SceneManager.GetActiveScene().name;
            
            await SceneManager.LoadSceneAsync(SceneRouterModel.Title, LoadSceneMode.Additive).ToUniTask();
            
            SceneManager.SetActiveScene(SceneManager.GetSceneByName(SceneRouterModel.Title));
            SceneManager.UnloadSceneAsync(sceneName);
        }
    }
    
    
}
