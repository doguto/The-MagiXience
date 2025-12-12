using UnityEngine;
using UnityEngine.AddressableAssets;
using Cysharp.Text;
using Project.Scripts.Extensions;

namespace Project.Scripts.Repository.AssetRepository
{
    public class FaceAssetRepository : AssetRepositoryBase
    {
        public Sprite Load(string charaName, string expression)
        {
            string address = ZString.Format(
                "{0}/Character/{1}/Face/{1}{2}_Face.png",
                GamePath.TexturesPath,
                charaName,
                $"_{expression}"
            );

            Sprite asset = Addressables.LoadAssetAsync<Sprite>(address).WaitForCompletion();
            return asset;
        }
    }
}
