using Project.Scripts.Model;

namespace Project.Scripts.Repository.ModelRepository
{
    public class UserModelRepository
    {
        public static UserModelRepository Instance { get; } = new();

        UserModel userModel = new();

        public UserModel Get()
        {
            return userModel ??= new();
        }
    }
}
