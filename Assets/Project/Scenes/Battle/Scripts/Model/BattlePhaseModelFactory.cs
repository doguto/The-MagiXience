using System;

namespace Project.Scenes.Battle.Scripts.Model
{
    public static class BattlePhaseModelFactory
    {
        public static BattlePhaseModelBase Create(BattlePhaseDefinition definition)
        {
            return definition.ExitCondition switch
            {
                BattlePhaseExitCondition.TimeLimit => new TimeLimitBattlePhaseModel(definition),
                _ => throw new NotSupportedException($"Exit condition {definition.ExitCondition} is not supported yet."),
            };
        }
    }
}
