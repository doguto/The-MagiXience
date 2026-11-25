using System;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.BehaviorTree
{
    [Serializable]
    public sealed class Constant : Condition
    {
        [SerializeField] bool value;

        public override string Summary => value ? "常に成立" : "成立しない";
        public override bool Evaluate(BattleContext ctx) => value;
    }
}
