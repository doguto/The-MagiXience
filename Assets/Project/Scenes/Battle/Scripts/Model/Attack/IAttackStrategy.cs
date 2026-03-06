using System;
using UniRx;

namespace Project.Scenes.Battle.Scripts.Model.Attack
{
    public interface IAttackStrategy : IDisposable
    {
        IObservable<AttackEvent> OnAttackTiming { get; }
        void Initialize();
        void Update(float deltaTime);
    }
}
