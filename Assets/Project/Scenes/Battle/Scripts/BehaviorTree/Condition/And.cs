using System;
using System.Collections.Generic;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.BehaviorTree
{
    [Serializable]
    public sealed class And : Condition
    {
        [SerializeReference, SubclassSelector] List<Condition> conditions = new();

        public override string Summary => Conditions.Join(conditions, " かつ ");

        protected override void OnSetup(TreeScope scope) => Conditions.Setup(conditions, scope);
        public override void Reset(BattleContext ctx) => Conditions.Reset(conditions, ctx);

        public override bool Evaluate(BattleContext ctx)
        {
            foreach (var c in conditions)
            {
                if (c != null && !c.Evaluate(ctx)) return false;
            }
            return true;
        }
    }
}
