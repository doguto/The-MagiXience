using System;

namespace Project.Scenes.Battle.Scripts.Model.ExitCondition
{
    [Serializable]
    public class AllEnemiesDefeatedExitConditionConfig : IExitConditionConfig
    {
        public BattlePhaseModelBase CreatePhaseModel(BattlePhaseDefinition definition, IEnemyTracker enemyTracker)
        {
            return new AllEnemiesDefeatedBattlePhaseModel(definition, enemyTracker);
        }
    }
}
