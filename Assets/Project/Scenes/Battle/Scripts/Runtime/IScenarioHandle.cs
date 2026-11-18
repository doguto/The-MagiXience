using Project.Scenes.Scenario.Scripts.Model;

namespace Project.Scenes.Battle.Scripts.Runtime
{
    public interface IScenarioHandle
    {
        bool IsCompleted { get; }
        bool TryDequeueCommand(out ScenarioStep command);
        void Cancel();
    }
}
