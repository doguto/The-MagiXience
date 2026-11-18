using UnityEngine;
using UnityEngine.AddressableAssets;
using Cysharp.Text;
using Project.Scripts.Extensions;

namespace Project.Scripts.Repository.AssetRepository
{
    public class StillAssetRepository : AssetRepositoryBase
    {
        // 立ち絵が存在しない場合（発狂差分が用意されていないキャラ等）はnullを返す
        public Sprite Load(string charaName, bool isCrazy)
        {
            string address = ZString.Format(
                "{0}/Character/{1}/Still/{1}{2}_Still.png",
                GamePath.TexturesPath,
                charaName,
                isCrazy ? "_Crazy" : ""
            );

            // 存在しないキーをロードするとInvalidKeyExceptionになるため、先にAddressに登録されているか確認する
            var locationsHandle = Addressables.LoadResourceLocationsAsync(address, typeof(Sprite));
            var locations = locationsHandle.WaitForCompletion();
            var exists = locations != null && locations.Count > 0;
            Addressables.Release(locationsHandle);
            if (!exists) return null;

            Sprite asset = Addressables.LoadAssetAsync<Sprite>(address).WaitForCompletion();
            return asset;
        }
    }
}
