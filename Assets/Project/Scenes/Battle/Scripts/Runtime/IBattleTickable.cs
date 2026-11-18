namespace Project.Scenes.Battle.Scripts.Runtime
{
    public interface IBattleTickable
    {
        bool IsFinished { get; }
        void BattleTick();
    }
}
