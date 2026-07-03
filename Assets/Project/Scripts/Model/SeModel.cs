using Project.Scripts.Infra;
using Project.Scripts.Repository.AssetRepository;
using UnityEngine;

namespace Project.Scripts.Model
{
    public class SeModel
    {
        readonly SoundAssetRepository soundAssetRepository = new();

        AudioClip audioClip;
        public AudioClip AudioClip => audioClip ?? LoadAudioClip();
        public SeData SeData { get; }
        public string Name => SeData.name;
        public int LoopStartSamples => SeData.loopStartSamples;
        public int LoopEndSamples => SeData.loopEndSamples;

        public SeModel(SeData seData)
        {
            SeData = seData;
        }

        public AudioClip LoadAudioClip()
        {
            audioClip = soundAssetRepository.LoadSE(SeData.seType);
            return audioClip;
        }
    }
}
