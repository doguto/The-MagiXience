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
        public GameSituation CurrentSituation { get; internal set; } = GameSituation.Way;

        public int GetScenarioNumber()
        {
            return (CurrentStageNumber - 1) * 2 + (CurrentSituation == GameSituation.Boss ? 2 : 1);
        }
        
        public void SetSituation(GameSituation situation)
        {
            CurrentSituation = situation;
        }
        
        public void AdvanceToNextPhase()
        {
            if (CurrentSituation == GameSituation.Way)
            {
                CurrentSituation = GameSituation.Boss;
            }
            else
            {
                CurrentStageNumber++;
                CurrentSituation = GameSituation.Way;
            }
        }

        public void ExitStage()
        {
            CurrentStageNumber = -1;
            CurrentSituation = GameSituation.Way;
        }
    }
}
