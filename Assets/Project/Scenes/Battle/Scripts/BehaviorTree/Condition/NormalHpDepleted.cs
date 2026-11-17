using System;
using Project.Scenes.Battle.Scripts.Model.Entity;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.BehaviorTree
{
    // 旧「発狂 (Strong)」の判定。通常 HP を削り切ったら成立
    [Serializable]
    public sealed class NormalHpDepleted : Condition
    {
        [SerializeField] HpTarget target = HpTarget.Self;

        public override string Summary => "通常 HP を削り切った";
        public override bool Evaluate(BattleContext ctx) => HpTargets.Resolve(ctx, target) is BossEntityModel boss && boss.ShouldUseStrongAttack;
    }
}
