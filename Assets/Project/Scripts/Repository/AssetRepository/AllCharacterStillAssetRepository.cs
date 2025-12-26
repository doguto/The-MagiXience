using Cysharp.Text;
using Project.Scripts.Extensions;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Project.Scripts.Repository.AssetRepository
{
    public class AllCharacterStillAssetRepository : AssetRepositoryBase
    {
        public Sprite Load(int characterCount)
        {
            var builder = ZString.CreateStringBuilder();
            builder.Append(GamePath.TexturesPath);
            builder.Append("/Character/All/All_");
            builder.Append(characterCount);
            builder.Append("_Still.png");
            var address = builder.ToString();

            var sprite = Addressables.LoadAssetAsync<Sprite>(address).WaitForCompletion();
            return sprite;
        }
    }
}
