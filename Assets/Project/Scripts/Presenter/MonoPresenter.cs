using JetBrains.Annotations;
using Project.Scenes.Global.Scripts.Presenter;
using Project.Scripts.Repository.ModelRepository;
using UnityEngine;

namespace Project.Scripts.Presenter
{
    public class MonoPresenter : MonoBehaviour
    {
        [CanBeNull] protected GlobalScenePresenter globalScenePresenter;
        [NotNull] protected SoundManagerPresenter soundManager;

        protected RuntimeModelRepository RuntimeModelRepository => RuntimeModelRepository.Instance;

        // 代入だとMonoランタイムの起動前にStageModelRepositoryのコンストラクタが呼ばれてしまうので、
        // getterで static Instance を呼ぶ。
        protected StageModelRepository StageModelRepository => StageModelRepository.Instance;

        // GlobalScenePresenterは常駐するため、全Presenterでシーン走査の結果を共有する。
        // 破棄された場合はUnityのnull判定で再検索される。
        static GlobalScenePresenter cachedGlobalScenePresenter;

        // RuntimeInitializeLoadType.BeforeSceneLoad より後に実行したいため、Start()
        protected virtual void Start()
        {
            if (cachedGlobalScenePresenter == null) cachedGlobalScenePresenter = FindFirstObjectByType<GlobalScenePresenter>();
            globalScenePresenter = cachedGlobalScenePresenter;
            if (!globalScenePresenter)
            {
                Debug.LogWarning("GlobalScenePresenterが見つかりません", this);
            }

            soundManager = globalScenePresenter!.SoundManagerPresenter;
        }
    }
}
