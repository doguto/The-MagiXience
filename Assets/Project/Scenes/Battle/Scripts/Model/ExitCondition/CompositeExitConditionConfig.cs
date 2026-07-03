using System;
using System.Collections.Generic;
using Project.Scenes.Battle.Scripts.Model.Entity;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Model.ExitCondition
{
    public enum CompositeMode
    {
        And,
        Or
    }

    [Serializable]
    public class CompositeExitConditionConfig : IExitConditionConfig
    {
        [SerializeField] CompositeMode mode = CompositeMode.And;

        [SerializeReference, SubclassSelector]
        List<IExitConditionConfig> conditions = new();

        public CompositeMode Mode => mode;
        public List<IExitConditionConfig> Conditions => conditions;

        public BattlePhaseModelBase CreatePhaseModel(BattlePhaseDefinition definition, IEnemyTracker enemyTracker, Func<EntityBase> getBossModel = null)
        {
            var innerModels = new List<BattlePhaseModelBase>(conditions.Count);
            foreach (var condition in conditions)
            {
                innerModels.Add(condition.CreatePhaseModel(definition, enemyTracker, getBossModel));
            }

            return new CompositeBattlePhaseModel(definition, mode, innerModels);
        }
    }
}
