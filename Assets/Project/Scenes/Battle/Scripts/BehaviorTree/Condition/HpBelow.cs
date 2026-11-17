using System;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.BehaviorTree
{
    // 旧 BossHpThreshold / HpPercentThreshold。現在 HP ≦ ceil(最大 HP × 割合)
    [Serializable]
    public sealed class HpBelow : Condition
    {
        [SerializeField] HpTarget target = HpTarget.Self;
        [SerializeField, Range(0f, 100f)] float percent = 50f;

        public override string Summary => $"{(target == HpTarget.Boss ? "ボス" : "")}HP ≦ {percent:0.#}%";

        public override bool Evaluate(BattleContext ctx)
        {
            var model = HpTargets.Resolve(ctx, target);
            if (model == null) return true;
            return model.CurrentHp.Value <= Mathf.CeilToInt(model.MaxHp * percent / 100f);
        }
    }
}
