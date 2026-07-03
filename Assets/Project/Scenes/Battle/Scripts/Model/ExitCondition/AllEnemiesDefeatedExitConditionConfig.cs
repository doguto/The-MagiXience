using System;
using Project.Scenes.Battle.Scripts.Model.Entity;

namespace Project.Scenes.Battle.Scripts.Model.ExitCondition
{
    [Serializable]
    public class AllEnemiesDefeatedExitConditionConfig : IExitConditionConfig
    {
        public BattlePhaseModelBase CreatePhaseModel(BattlePhaseDefinition definition, IEnemyTracker enemyTracker, Func<EntityBase> getBossModel = null)
        {
            return new AllEnemiesDefeatedBattlePhaseModel(definition, enemyTracker);
        }
    }
}
