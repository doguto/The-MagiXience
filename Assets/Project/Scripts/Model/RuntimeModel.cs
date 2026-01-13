namespace Project.Scripts.Model
{
    public enum GameSituation
    {
        Way,
        Boss
    }

    public class RuntimeModel : ModelBase
    {
        public int CurrentStageNumber { get; internal set; } = 1;
        public bool IsInGame => CurrentStageNumber != -1;
        public GameSituation CurrentSituation { get; internal set; } = GameSituation.Way;

        public int GetScenarioNumber()
        {
            return (CurrentStageNumber - 1) * 2 + (CurrentSituation == GameSituation.Boss ? 2 : 1);
        }

        public void SetSituation(GameSituation situation)
        {
            CurrentSituation = situation;
        }

        public void AdvanceToNextSequence()
        {
            if (CurrentSituation == GameSituation.Way)
            {
                CurrentSituation = GameSituation.Boss;
                UnityEngine.Debug.Log($"[RuntimeModel] Stage {CurrentStageNumber}: Way → Boss");
            }
            else
            {
                CurrentStageNumber++;
                CurrentSituation = GameSituation.Way;
                UnityEngine.Debug.Log($"[RuntimeModel] Stage advanced: {CurrentStageNumber - 1} → {CurrentStageNumber}");
            }
        }

        public void ExitStage()
        {
            CurrentStageNumber = -1;
            CurrentSituation = GameSituation.Way;
        }
    }
}
