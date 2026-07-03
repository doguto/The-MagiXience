using Project.Scripts.Extensions;

namespace Project.Scripts.Model
{
    public enum BattleSituation
    {
        Way,
        Boss
    }

    public class RuntimeModel : ModelBase
    {
        public BattleStageType CurrentStageType { get; set; } = BattleStageType.Null;
        public bool IsInGame => CurrentStageType != BattleStageType.Null;
        public BattleSituation CurrentSituation { get; set; } = BattleSituation.Way;

        public int GetScenarioNumber()
        {
            return (CurrentStageType.AsInt() - 1) * 2 + (CurrentSituation == BattleSituation.Boss ? 2 : 1);
        }

        public void SetSituation(BattleSituation situation)
        {
            CurrentSituation = situation;
        }

        public void AdvanceToNextSequence()
        {
            if (CurrentSituation == BattleSituation.Way)
            {
                CurrentSituation = BattleSituation.Boss;
                UnityEngine.Debug.Log($"[RuntimeModel] Stage {CurrentStageType.AsInt()}: Way → Boss");
            }
            else
            {
                CurrentStageType = CurrentStageType.Next();
                CurrentSituation = BattleSituation.Way;
                UnityEngine.Debug.Log($"[RuntimeModel] Stage advanced: {CurrentStageType.AsInt() - 1} → {CurrentStageType.AsInt()}");
            }
        }

        public void ExitStage()
        {
            CurrentStageType = BattleStageType.Null;
            CurrentSituation = BattleSituation.Way;
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public void SetForDebug(int stageNumber, BattleSituation situation)
        {
            CurrentStageType = BattleStageTypeExtensions.FromInt(stageNumber);
            CurrentSituation = situation;
        }
#endif
    }
}
