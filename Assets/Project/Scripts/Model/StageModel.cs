using System;
using Project.Scripts.Infra;
using Project.Scripts.Repository.AssetRepository;
using UnityEngine;

namespace Project.Scripts.Model
{
    public class StageModel : ModelBase
    {
        public StageData StageData { get; }

        public Sprite CharaImage { get; }
        public bool IsOpened { get; private set; }
        public bool IsCleared { get; private set; }

        readonly UserModel userModel;

        public StageModel(UserModel userModel, StageData stageData, bool isOpened = false, bool isCleared = false)
        {
            this.userModel = userModel;
            StageData = stageData;
            IsOpened = isOpened;
            IsCleared = isCleared && isOpened; // 念のため isOpened と AND する

            var stillAssetRepository = new StillAssetRepository();
            CharaImage = stillAssetRepository.Load(StageData.charaStillAddress, false);
        }

        public void Start()
        {
            userModel.CurrentStageNumber = StageData.stageNumber;
        }

        public void Open()
        {
            IsOpened = true;
        }

        public void Clear()
        {
            if (!IsOpened) throw new Exception("ステージが開放されていません.");

            IsCleared = true;
            userModel.StageClear(StageData.stageNumber);
            userModel.CurrentStageNumber = -1;
        }

        public (string id, string title) GetIdAndTitle()
        {
            return (StageData.id, StageData.title);
        }
    }
}
