using JetBrains.Annotations;
using Project.Scenes.Global.Scripts.Presenter;
using Project.Scripts.Repository.ModelRepository;
using UnityEngine;

namespace Project.Scripts.Presenter
{
    public class MonoPresenter : MonoBehaviour
    {
        [CanBeNull] protected GlobalScenePresenter globalScenePresenter;
        [CanBeNull] protected SoundManagerPresenter soundManager;

        protected RuntimeModelRepository RuntimeModelRepository => RuntimeModelRepository.Instance;

        // 代入だとMonoランタイムの起動前にStageModelRepositoryのコンストラクタが呼ばれてしまうので、
        // getterで static Instance を呼ぶ。
        protected StageModelRepository StageModelRepository => StageModelRepository.Instance;

        // RuntimeInitializeLoadType.BeforeSceneLoad より後に実行したいため、Start()
        protected virtual void Start()
        {
            globalScenePresenter = FindFirstObjectByType<GlobalScenePresenter>();
            if (!globalScenePresenter)
            {
                Debug.LogWarning("GlobalScenePresenterが見つかりません", this);
            }

            soundManager = globalScenePresenter!.SoundManagerPresenter;
        }
    }
}
