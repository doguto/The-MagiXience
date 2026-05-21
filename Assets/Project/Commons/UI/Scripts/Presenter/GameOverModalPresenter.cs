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
    public class GameOverModalPresenter : MonoPresenter
    {
        [SerializeField] GameOverModalView gameOverModalView;
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

            gameOverModalView.OnPressedRetry.Subscribe(_ => Retry()).AddTo(this);
            gameOverModalView.OnPressedOption.Subscribe(_ =>
            {
                globalScenePresenter.OptionModalPresenter.Open();
                gameObject.SetActive(false);

                pauseEvent?.Dispose();
                pauseEvent = globalScenePresenter.OptionModalPresenter.OnClosed.Subscribe(_ =>
                {
                    gameObject.SetActive(true);
                    gameOverModalView.InitStart();
                    pauseEvent?.Dispose();
                    pauseEvent = null;
                });
            }).AddTo(this);
            gameOverModalView.OnPressedTitle.Subscribe(_ => LoadTitle().Forget()).AddTo(this);

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
            gameOverModalView.InitStart();
        }

        void Retry()
        {
            // 時間/音を戻してモーダルを閉じる。実際の再初期化はOnRetryRequestedを購読する側（BattleScenePresenter）が行う。
            IsOpen = false;
            Time.timeScale = 1f;
            AudioListener.pause = false;
            gameObject.SetActive(false);

            onRetryRequested.OnNext(Unit.Default);
        }

        async UniTaskVoid LoadTitle()
        {
            IsOpen = false;
            Time.timeScale = 1f;
            AudioListener.pause = false;
            gameObject.SetActive(false);

            var sceneName = SceneManager.GetActiveScene().name;
            await SceneManager.LoadSceneAsync(SceneRouterModel.Title, LoadSceneMode.Additive).ToUniTask();
            SceneManager.SetActiveScene(SceneManager.GetSceneByName(SceneRouterModel.Title));
            SceneManager.UnloadSceneAsync(sceneName).ToUniTask().Forget();
        }
    }
}
