using System.Collections.Generic;
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

        void Start()
        {
            base.Start();

            stageCardListView.Init();
            for (var i = 0; i < stageModels.Count; i++)
            {
                stageCardListView.stageCardViews[i].Setup(stageModels[i].GetIdAndTitle());
            }

            ShowCharaImage(0);
            stageCardListView.OnButtonChanged.Subscribe(ShowCharaImage);
        }

        void ShowCharaImage(int buttonIndex)
        {
            var charaImage = stageModels[buttonIndex].CharaImage;
            stageCardListView.SetCharaImage(charaImage);
        }
    }
}
