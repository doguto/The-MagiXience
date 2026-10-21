using System;
using Project.Scenes.Battle.Scripts.Model.Entity;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Model.ExitCondition
{
    [Serializable]
    public class TimeLimitExitConditionConfig : IExitConditionConfig
    {
        [SerializeField, Min(0.1f)] float timeLimitSeconds = 10f;

        public TimeLimitExitConditionConfig() { }

        /// <summary>コードから動的に組み立てる用(例: interludeの終了時間上書き)。</summary>
        public TimeLimitExitConditionConfig(float timeLimitSeconds)
        {
            this.timeLimitSeconds = timeLimitSeconds;
        }

        public BattlePhaseModelBase CreatePhaseModel(BattlePhaseDefinition definition, IEnemyTracker enemyTracker, Func<EntityBase> getBossModel = null)
        {
            return new TimeLimitBattlePhaseModel(definition, timeLimitSeconds);
        }
    }
}
