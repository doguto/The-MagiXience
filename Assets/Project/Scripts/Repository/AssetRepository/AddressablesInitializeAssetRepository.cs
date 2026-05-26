using Cysharp.Threading.Tasks;
using UnityEngine.AddressableAssets;

namespace Project.Scripts.Repository.AssetRepository
{
    public class AddressablesInitializeAssetRepository : AssetRepositoryBase
    {
        public async UniTask InitializeAsync()
        {
            await Addressables.InitializeAsync().ToUniTask();
        }
    }
}
