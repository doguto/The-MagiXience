using Project.Scripts.Model;

namespace Project.Scripts.Repository.ModelRepository
{
    public class UserModelRepository
    {
        public static UserModelRepository Instance { get; } = new();

        readonly RuntimeModelRepository runtimeModelRepository;
        UserModel userModel;

        public UserModelRepository()
        {
            runtimeModelRepository = RuntimeModelRepository.Instance;
            userModel = new(){ RuntimeModel = runtimeModelRepository.Get() };
        }
        
        public UserModel Get()
        {
            return userModel ??= new(){ RuntimeModel = runtimeModelRepository.Get() };
        }
    }
}
