namespace Project.Scenes.Battle.Scripts.Model.Movement
{
    public interface IMovementConfig
    {
        IMovementStrategy CreateStrategy();
    }
}
