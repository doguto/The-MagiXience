using System;
using Project.Scenes.Battle.Scripts.Model.Entity;

namespace Project.Scenes.Battle.Scripts.Model.PhaseNode
{
    // ボスの通常HPが尽きていれば true
    [Serializable]
    public class BossStrongCondition : IBranchCondition
    {
        public bool Evaluate(PhaseNodeContext context)
        {
            return context.BossModel is BossEntityModel boss && boss.ShouldUseStrongAttack;
        }
    }
}
