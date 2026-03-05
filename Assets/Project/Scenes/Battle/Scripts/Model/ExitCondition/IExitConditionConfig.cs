namespace Project.Scenes.Battle.Scripts.Model.ExitCondition
{
    public interface IExitConditionConfig
    {
        BattlePhaseModelBase CreatePhaseModel(BattlePhaseDefinition definition, IEnemyTracker enemyTracker);
    }
}
