using Project.Scripts.Model;

namespace Project.Scripts.Repository.ModelRepository
{
    public class KeyConfigModelRepository
    {
        public static KeyConfigModelRepository Instance { get; } = new();

        readonly UserModelRepository userModelRepository;
        KeyConfigModel keyConfigModel;

        public KeyConfigModelRepository()
        {
            userModelRepository = UserModelRepository.Instance;
        }

        public KeyConfigModel Get()
        {
            if (keyConfigModel != null) return keyConfigModel;
            
            var userModel = userModelRepository.Get();
            keyConfigModel = new KeyConfigModel(userModel);
            return keyConfigModel;
        }
        
        public void Refresh()
        {
            keyConfigModel = null;
        }
    }
}
