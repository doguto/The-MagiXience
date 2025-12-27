namespace Project.Scenes.Battle.Scripts.Model
{
    public readonly struct ScenarioTransitionRequest
    {
        public ScenarioTransitionRequest(int stageNumber, string scenarioId, ScenarioTransitionTiming timing)
        {
            StageNumber = stageNumber;
            ScenarioId = scenarioId;
            Timing = timing;
        }

        public int StageNumber { get; }
        public string ScenarioId { get; }
        public ScenarioTransitionTiming Timing { get; }
        public bool IsValid => !string.IsNullOrEmpty(ScenarioId);
    }

    public enum ScenarioTransitionTiming
    {
        WayToBoss,
        BossToNextStage
    }
}
