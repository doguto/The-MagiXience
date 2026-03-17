using System;
using Project.Scenes.Battle.Scripts.Model.Entity;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Model.ExitCondition
{
    [Serializable]
    public class BgmPositionExitConditionConfig : IExitConditionConfig
    {
        [SerializeField] int thresholdSamples;

        public Func<AudioSource> GetBgmAudioSource { get; set; }

        public BattlePhaseModelBase CreatePhaseModel(
            BattlePhaseDefinition definition,
            IEnemyTracker enemyTracker,
            Func<EntityBase> getBossModel = null)
        {
            return new BgmPositionBattlePhaseModel(definition, thresholdSamples, GetBgmAudioSource);
        }
    }
}
