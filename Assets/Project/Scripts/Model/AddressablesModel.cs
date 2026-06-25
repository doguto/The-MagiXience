using Cysharp.Threading.Tasks;
using Project.Scripts.Repository.AssetRepository;

namespace Project.Scripts.Model
{
    public class AddressablesModel : ModelBase
    {
        readonly AddressablesInitializeAssetRepository addressablesInitializeAssetRepository = new();

        public async UniTask InitializeAsync()
        {
            await addressablesInitializeAssetRepository.InitializeAsync();
        }
    }
}
