namespace Project.Scripts.Model
{
    public enum BattleSituation
    {
        Way,
        Boss
    }

    public class RuntimeModel : ModelBase
    {
        public int CurrentStageNumber { get; internal set; } = -1;
        public bool IsInGame => CurrentStageNumber != -1;
        public BattleSituation CurrentSituation { get; set; } = BattleSituation.Way;

        public int GetScenarioNumber()
        {
            return (CurrentStageNumber - 1) * 2 + (CurrentSituation == BattleSituation.Boss ? 2 : 1);
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
                UnityEngine.Debug.Log($"[RuntimeModel] Stage {CurrentStageNumber}: Way → Boss");
            }
            else
            {
                CurrentStageNumber++;
                CurrentSituation = BattleSituation.Way;
                UnityEngine.Debug.Log($"[RuntimeModel] Stage advanced: {CurrentStageNumber - 1} → {CurrentStageNumber}");
            }
        }

        public void ExitStage()
        {
            CurrentStageNumber = -1;
            CurrentSituation = BattleSituation.Way;
        }
    }
}
