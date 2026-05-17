using System;
using Cysharp.Threading.Tasks;
using Project.Commons.UI.Scripts.View;
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
        readonly Subject<Unit> onRetryRequested = new();
        public IObservable<Unit> OnClosed => onClosed;
        public IObservable<Unit> OnRetryRequested => onRetryRequested;
        public bool IsOpen { get; private set; }

        RuntimeModel runtimeModel;

        void Awake()
        {
            runtimeModel = RuntimeModelRepository.Get();
        }

        protected override void Start()
        {
            base.Start();
            
            pauseModalView.InitStart();

            pauseModalView.OnPressedCancel.Subscribe(_ =>
            {
                Close();
            });
            pauseModalView.OnPressedRetry.Subscribe(_ =>
            {
                IsOpen = false;
                Time.timeScale = 1f;
                AudioListener.pause = false;
                gameObject.SetActive(false);
                onRetryRequested.OnNext(Unit.Default);
            });
            pauseModalView.OnPressedOption.Subscribe(_ =>
            {
                globalScenePresenter.OptionModalPresenter.Open();
                gameObject.SetActive(false);

                pauseEvent?.Dispose();
                pauseEvent = globalScenePresenter.OptionModalPresenter.OnClosed.Subscribe(_ =>
                {
                    gameObject.SetActive(true);
                    pauseModalView.InitStart();
                    pauseEvent?.Dispose();
                    pauseEvent = null;
                });
            });
            pauseModalView.OnPressedExit.Subscribe(x => LoadTitle(x).Forget()).AddTo(this);

            // Globalシーンで起動された時点では非表示にしておく
            if (!IsOpen)
            {
                gameObject.SetActive(false);
            }
        }

        public void Open()
        {
            if (IsOpen) return;
            IsOpen = true;

            Time.timeScale = 0f;
            AudioListener.pause = true;

            gameObject.SetActive(true);
            pauseModalView.InitStart();
        }

        public void Close()
        {
            if (!IsOpen) return;
            IsOpen = false;

            Time.timeScale = 1f;
            AudioListener.pause = false;

            gameObject.SetActive(false);
            onClosed.OnNext(Unit.Default);
        }

        async UniTask LoadTitle(Unit _)
        {
            Close();
            
            var sceneName = SceneManager.GetActiveScene().name;

            await SceneManager.LoadSceneAsync(SceneRouterModel.Title, LoadSceneMode.Additive).ToUniTask();

            SceneManager.SetActiveScene(SceneManager.GetSceneByName(SceneRouterModel.Title));
            SceneManager.UnloadSceneAsync(sceneName).ToUniTask().Forget();
        }
    }
}
