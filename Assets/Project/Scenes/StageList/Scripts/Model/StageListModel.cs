using Project.Scripts.Model;
using Project.Scripts.Repository.AssetRepository;
using UnityEngine;

namespace Project.Scenes.StageList.Scripts.Model
{
    public class StageListModel : ModelBase
    {
        readonly BackGroundAssetRepository backGroundAssetRepository = new();

        readonly int clearedStageNumber;
        Sprite backGroundSprite;

        public StageListModel(int clearedStageNumber)
        {
            this.clearedStageNumber = clearedStageNumber;
        }

        public Sprite GetBackGroundSprite()
        {
            backGroundSprite ??= backGroundAssetRepository.Load(clearedStageNumber + 1);
            return backGroundSprite;
        }
    }
}
