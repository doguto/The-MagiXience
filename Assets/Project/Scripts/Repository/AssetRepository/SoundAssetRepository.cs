using Cysharp.Text;
using Project.Scripts.Extensions;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Project.Scripts.Repository.AssetRepository
{
    public class SoundAssetRepository : AssetRepositoryBase
    {
        public AudioClip LoadBGM(SceneType sceneType, BgmType bgmType)
        {
            var address = ZString.Format(
                "{0}/Sounds/bgm/{1}_{2}",
                GamePath.TexturesPath,
                sceneType.ToString(),
                bgmType.ToString()
            );

            var audioClip = Addressables.LoadAssetAsync<AudioClip>(address).WaitForCompletion();
            return audioClip;
        }

        public AudioClip LoadSE(string seName)
        {
            var address = ZString.Format(
                "{0}/Sounds/se/{1}",
                GamePath.TexturesPath,
                seName
            );

            var audioClip = Addressables.LoadAssetAsync<AudioClip>(address).WaitForCompletion();
            return audioClip;
        }
    }
}
