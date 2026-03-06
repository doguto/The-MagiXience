namespace Project.Scenes.Battle.Scripts.Model.Attack
{
    public interface IAttackConfig
    {
        IAttackStrategy CreateStrategy(IPlayerPositionProvider playerPositionProvider);
    }
}
