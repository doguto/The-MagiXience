using System;
using Project.Scripts.Extensions;
using Project.Scripts.Infra;
using Project.Scripts.Repository.AssetRepository;
using UnityEngine;

namespace Project.Scripts.Model
{
    public class StageModel : ModelBase
    {
        public StageData StageData { get; }

        public int StageNumber => StageData.stageNumber;
        public BattleStageType BattleStageType => BattleStageTypeExtensions.FromInt(StageData.stageNumber);
        public string WaySequenceAddress => StageData.waySequenceAddress;
        public string BossSequenceAddress => StageData.bossSequenceAddress;
        public string BackgroundAddress => StageData.backgroundAddress;

        public Sprite CharaImage { get; }
        public bool IsOpened { get; private set; }
        public bool IsCleared { get; private set; }

        public StageModel(StageData stageData, bool isOpened = false, bool isCleared = false)
        {
            StageData = stageData;
            IsOpened = isOpened;
            IsCleared = isCleared && isOpened; // 念のため isOpened と AND する

            var stillAssetRepository = new StillAssetRepository();
            CharaImage = stillAssetRepository.Load(StageData.charaStillAddress, false);
        }

        public void Start()
        {
            RuntimeModel.CurrentStageType = BattleStageTypeExtensions.FromInt(StageData.stageNumber);
        }

        public void Open()
        {
            IsOpened = true;
        }

        public void Clear()
        {
            if (!IsOpened) throw new Exception("ステージが開放されていません.");

            IsCleared = true;
            UserModel.StageClear(StageData.stageNumber);
            // TODO: ステージ進行用と、タイトルに戻る用の２つのエントリーポイントを用意する
            // RuntimeModel.ExitStage();
        }

        public (string id, string title) GetIdAndTitle()
        {
            return (StageData.id, StageData.title);
        }
    }
}
