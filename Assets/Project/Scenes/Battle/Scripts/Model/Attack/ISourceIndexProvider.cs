namespace Project.Scenes.Battle.Scripts.Model.Attack
{
    public interface ISourceIndexProvider
    {
        int Get();
        ISourceIndexProvider Clone();
    }
}
