using System;
using Project.Scenes.Battle.Scripts.Model.Entity;

namespace Project.Scenes.Battle.Scripts.Model.ExitCondition
{
    public interface IExitConditionConfig
    {
        BattlePhaseModelBase CreatePhaseModel(BattlePhaseDefinition definition, IEnemyTracker enemyTracker, Func<EntityBase> getBossModel = null);
    }
}
