using Project.Scripts.Extensions;
using Project.Scripts.Model;
using Project.Scripts.Repository.AssetRepository;
using UnityEngine;

namespace Project.Scenes.Global.Scripts.Model
{
    public class SoundModel : ModelBase
    {
        readonly SoundAssetRepository soundAssetRepository = new();

        AudioClip currentBgmClip;
        AudioClip currentSeClip;

        public AudioClip GetBGM(SceneType sceneType, BgmType bgmType)
        {
            currentBgmClip = soundAssetRepository.LoadBGM(sceneType, bgmType);
            return currentBgmClip;
        }

        public AudioClip GetSE(string seName)
        {
            currentSeClip = soundAssetRepository.LoadSE(seName);
            return currentSeClip;
        }
    }
}
