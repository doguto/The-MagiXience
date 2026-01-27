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
            string address = ZString.Format(
                "{0}/Sounds/BGM/{1}_{2}",
                GamePath.TexturesPath,
                sceneType.ToString(),
                bgmType.ToString()
            );

            AudioClip audioClip = Addressables.LoadAssetAsync<AudioClip>(address).WaitForCompletion();
            return audioClip;
        }

        public AudioClip LoadSE(string seName)
        {
            string address = ZString.Format(
                "{0}/Sounds/SE/{1}",
                GamePath.TexturesPath,
                seName
            );

            AudioClip audioClip = Addressables.LoadAssetAsync<AudioClip>(address).WaitForCompletion();
            return audioClip;
        }
    }
}
