using Project.Scenes.Title.Scripts.Model;
using Project.Scripts.Model;
using Project.Scripts.Repository.ModelRepository;

namespace Project.Scenes.Title.Scripts.Repository.ModelRepository
{
    public class TitleModelRepository : ModelRepositoryBase
    {
        public static TitleModelRepository Instance { get; } = new();

        TitleModel titleModel;

        public TitleModelRepository()
        {
            titleModel = new(UserModel.ClearedStageNumber);
        }

        public TitleModel Get()
        {
            titleModel ??= new(UserModel.ClearedStageNumber);
            return titleModel;
        }

        public void Refresh()
        {
            titleModel = null;
        }
    }
}
