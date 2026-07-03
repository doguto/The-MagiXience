using Cysharp.Text;
using Project.Scripts.Extensions;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Project.Scripts.Repository.AssetRepository
{
    public class BackGroundAssetRepository : AssetRepositoryBase
    {
        
        public Sprite Load(int count)
        {
            var builder = ZString.CreateStringBuilder();
            builder.Append(GamePath.TexturesPath);
            builder.Append("/Images/BackGround/BackGround_");
            builder.Append(count);
            builder.Append(".png");
            string address = builder.ToString();

            Sprite sprite = Addressables.LoadAssetAsync<Sprite>(address).WaitForCompletion();
            return sprite;
        }
    }
}
