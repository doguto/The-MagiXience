using Project.Scripts.Model;
using Project.Scripts.Repository.AssetRepository;
using UnityEngine;

namespace Project.Scenes.Title.Scripts.Model
{
    public class TitleModel : ModelBase
    {
        readonly AllCharacterStillAssetRepository allCharacterStillAssetRepository = new();
        readonly BackGroundAssetRepository backGroundAssetRepository = new();

        readonly int clearedStageNumber;
        Sprite memberStillSprite;
        Sprite backGroundSprite;

        public TitleModel(int clearedStageNumber)
        {
            this.clearedStageNumber = clearedStageNumber;
        }

        public (Sprite memberStill, Sprite backGround)GetBackGroundSprites()
        {
            var characterCount = clearedStageNumber + 1;
            memberStillSprite ??= allCharacterStillAssetRepository.Load(characterCount);
            backGroundSprite ??= backGroundAssetRepository.Load(characterCount);
            return (memberStillSprite, backGroundSprite);
        }
    }
}
