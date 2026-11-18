using UniRx;

namespace Project.Scenes.Battle.Scripts.Model.Attack.PhaseTrigger
{
    public readonly struct AttackPhaseTriggerContext
    {
        public readonly IReadOnlyReactiveProperty<int> CurrentHp;
        public readonly int MaxHp;

        public AttackPhaseTriggerContext(IReadOnlyReactiveProperty<int> currentHp, int maxHp)
        {
            CurrentHp = currentHp;
            MaxHp = maxHp;
        }
    }
}
