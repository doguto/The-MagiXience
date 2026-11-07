namespace Project.Scenes.Battle.Scripts.Model.PhaseAction
{
    public interface IPhaseAction
    {
    }

    // フェーズ開始からの経過時間で発火するアクション
    public interface ITimedPhaseAction : IPhaseAction
    {
        float Time { get; }
    }
}
