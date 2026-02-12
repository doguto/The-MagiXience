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
                "{0}/Sounds/Bgm/{1}_{2}",
                GamePath.TexturesPath,
                sceneType.ToSceneName(),
                bgmType.ToString()
            );

            var audioClip = Addressables.LoadAssetAsync<AudioClip>(address).WaitForCompletion();
            return audioClip;
        }

        public AudioClip LoadSE(SeType seType)
        {
            var address = ZString.Format(
                "{0}/Sounds/Se/{1}",
                GamePath.TexturesPath,
                seType.ToString()
            );

            var audioClip = Addressables.LoadAssetAsync<AudioClip>(address).WaitForCompletion();
            return audioClip;
        }
    }
}
