using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Model.Attack
{
    public interface IAttackStrategy
    {
        void Update(float deltaTime);
        void Initialize();
        bool CanAttack { get; }
    }
}
