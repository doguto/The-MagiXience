using System;
using Project.Scenes.Battle.Scripts.Model.Entity;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Model.ExitCondition
{
    [Serializable]
    public class BossHpThresholdExitConditionConfig : IExitConditionConfig
    {
        [SerializeField, Range(0f, 100f)] float hpThresholdPercent = 50f;

        public BattlePhaseModelBase CreatePhaseModel(BattlePhaseDefinition definition, IEnemyTracker enemyTracker, Func<EntityBase> getBossModel = null)
        {
            return new BossHpThresholdBattlePhaseModel(definition, hpThresholdPercent / 100f, getBossModel);
        }
    }
}
