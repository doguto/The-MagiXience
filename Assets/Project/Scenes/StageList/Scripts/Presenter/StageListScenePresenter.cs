using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Project.Scenes.StageList.Scripts.Model;
using Project.Scenes.StageList.Scripts.Repository.ModelRepository;
using Project.Scripts.Model;
using Project.Scenes.StageList.Scripts.View;
using Project.Scripts.Extensions;
using Project.Scripts.Extensions.Message;
using Project.Scripts.Presenter;
using UniRx;
using UnityEngine;

namespace Project.Scenes.StageList.Scripts.Presenter
{
    public class StageListScenePresenter : MonoPresenter
    {
        [SerializeField] StageCardListView stageCardListView;
        [SerializeField] StageListSceneView stageListSceneView;

        List<StageModel> stageModels;
        StageListModel stageListModel;
        StageListModelRepository stageListModelRepository;

        void Awake()
        {
            stageModels = StageModelRepository.GetAll();
            stageListModelRepository = StageListModelRepository.Instance;
            stageListModel = stageListModelRepository.Get();
            stageListSceneView.SetBackGround(stageListModel.GetBackGroundSprite());
        }

        protected override void Start()
        {
            base.Start();

            var isOpenedList = stageModels.Select(m => m.IsOpened).ToList();
            stageCardListView.Init(isOpenedList);
            for (var i = 0; i < stageModels.Count; i++)
            {
                stageCardListView.stageCardViews[i].Setup(stageModels[i].GetIdAndTitle(), stageModels[i].IsOpened);
            }

            ShowCharaImage(0);
            stageCardListView.OnButtonChanged.Subscribe(ShowCharaImage);
            stageCardListView.OnButtonPressed.Subscribe(i => LoadBattleScene(i).Forget());
            MessageBroker.Default.Receive<UICancelMessage>()
                .Subscribe(_ => BackToTitle().Forget())
                .AddTo(this);
        }

        void ShowCharaImage(int buttonIndex)
        {
            var charaImage = stageModels[buttonIndex].CharaImage;
            var isCleared = stageModels[buttonIndex].IsCleared;
            stageCardListView.SetCharaImage(charaImage, isCleared);
        }

        async UniTask BackToTitle()
        {
            soundManager.PlaySEAsync(SeType.Cancel).Forget();
            await globalScenePresenter.SceneNavigator.NavigateTo(SceneRouterModel.Title, SceneRouterModel.StageList);
        }

        async UniTask LoadBattleScene(int buttonIndex)
        {
            // DEMO: 1面以外のバトルシーンをロードできないようにする
            if (buttonIndex != 0) return;
            soundManager.PlaySEAsync(SeType.Click).Forget();

            var runtimeModel = RuntimeModelRepository.Get();
            runtimeModel.CurrentStageType = stageModels[buttonIndex].BattleStageType;
            runtimeModel.CurrentSituation = BattleSituation.Way;

            await globalScenePresenter.SceneNavigator.NavigateTo(SceneRouterModel.Battle, SceneRouterModel.StageList);
        }
    }
}
