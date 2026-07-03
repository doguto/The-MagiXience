using Project.Scenes.StageList.Scripts.Model;
using Project.Scripts.Repository.ModelRepository;

namespace Project.Scenes.StageList.Scripts.Repository.ModelRepository
{
    public class StageListModelRepository : ModelRepositoryBase
    {
        public static StageListModelRepository Instance { get; } = new();

        StageListModel stageListModel;

        public StageListModelRepository()
        {
            stageListModel = new(UserModel.ClearedStageNumber);
        }

        public StageListModel Get()
        {
            stageListModel ??= new(UserModel.ClearedStageNumber);
            return stageListModel;
        }

        public void Refresh()
        {
            stageListModel = null;
        }
    }
}
