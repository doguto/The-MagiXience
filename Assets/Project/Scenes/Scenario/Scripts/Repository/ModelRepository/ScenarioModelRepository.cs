using Project.Scenes.Scenario.Scripts.Model;
using Project.Scripts.Model;
using Project.Scripts.Repository.ModelRepository;

namespace Project.Scenes.Scenario.Scripts.Repository.ModelRepository
{
    public class ScenarioModelRepository : ModelRepositoryBase
    {
        public static ScenarioModelRepository Instance { get; } = new();

        ScenarioModel scenarioModel;

        public ScenarioModelRepository()
        {
        }

        public ScenarioModel Get()
        {
            return scenarioModel;
        }

        public void Refresh()
        {
            scenarioModel = null;
        }
    }
}
