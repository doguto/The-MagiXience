using Project.Scripts.Infra;
using Project.Scripts.Repository.AssetRepository;
using UnityEngine;

namespace Project.Scripts.Model
{
    public class BgmModel : ModelBase
    {
        readonly SoundAssetRepository soundAssetRepository = new();

        AudioClip audioClip;

        public BgmModel(BgmData bgmData)
        {
            BgmData = bgmData;
        }

        public AudioClip AudioClip => audioClip ?? LoadAudioClip();

        public BgmData BgmData { get; }
        public string Name => BgmData.name;

        AudioClip LoadAudioClip()
        {
            audioClip = soundAssetRepository.LoadBGM(BgmData.sceneType, BgmData.bgmType);
            return audioClip;
        }
    }
}
