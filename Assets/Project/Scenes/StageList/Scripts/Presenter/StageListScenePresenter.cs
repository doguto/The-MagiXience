using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Project.Scripts.Model;
using Project.Scenes.StageList.Scripts.View;
using Project.Scripts.Extensions;
using Project.Scripts.Presenter;
using UniRx;
using UnityEngine;

namespace Project.Scenes.StageList.Scripts.Presenter
{
    public class StageListScenePresenter : MonoPresenter
    {
        [SerializeField] StageCardListView stageCardListView;

        List<StageModel> stageModels;

        void Awake()
        {
            stageModels = StageModelRepository.GetAll();
        }

        protected override void Start()
        {
            base.Start();

            var isOpenedList = stageModels.Select(m => m.IsOpened).ToList();
            stageCardListView.Init(isOpenedList);
            for (var i = 0; i < stageModels.Count; i++)
            {
                stageCardListView.stageCardViews[i].Setup(stageModels[i].GetIdAndTitle());
            }

            ShowCharaImage(0);
            stageCardListView.OnButtonChanged.Subscribe(ShowCharaImage);
            stageCardListView.OnButtonPressed.Subscribe(i => LoadBattleScene(i).Forget());
        }

        void ShowCharaImage(int buttonIndex)
        {
            var charaImage = stageModels[buttonIndex].CharaImage;
            stageCardListView.SetCharaImage(charaImage);
        }

        async UniTask LoadBattleScene(int buttonIndex)
        {
            soundManager.PlaySEAsync(SeType.Click).Forget();

            var runtimeModel = RuntimeModelRepository.Get();
            runtimeModel.CurrentStageType = stageModels[buttonIndex].BattleStageType;
            runtimeModel.CurrentSituation = BattleSituation.Way;

            await SceneNavigator.NavigateTo(SceneRouterModel.Battle, SceneRouterModel.StageList);
        }
    }
}
