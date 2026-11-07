namespace Project.Scenes.Battle.Scripts.Model.PhaseNode
{
    public interface IBranchCondition
    {
        bool Evaluate(PhaseNodeContext context);
    }
}
