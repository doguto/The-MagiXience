using Project.Scripts.Model;

namespace Project.Scripts.Repository.ModelRepository
{
    public class RuntimeModelRepository : ModelRepositoryBase
    {
        public static RuntimeModelRepository Instance { get; } = new();

        RuntimeModel runtimeModel;

        public RuntimeModel Get()
        {
            runtimeModel ??= new();
            return runtimeModel;
        }

        public void Refresh()
        {
            runtimeModel = null;
        }
    }
}
