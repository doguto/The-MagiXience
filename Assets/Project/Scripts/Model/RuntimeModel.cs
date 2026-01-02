namespace Project.Scripts.Model
{
    public class RuntimeModel : ModelBase
    {
        public int CurrentStageNumber { get; internal set; } = -1;
        public bool IsInGame => CurrentStageNumber != -1;
    }
}
