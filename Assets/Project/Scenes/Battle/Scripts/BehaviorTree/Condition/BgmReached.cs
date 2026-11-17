using System;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.BehaviorTree
{
    [Serializable]
    public sealed class BgmReached : Condition
    {
        [SerializeField, Tooltip("BGM の再生位置(秒)。ループを含む累計時刻で判定する")] BtFloat seconds;

        [NonSerialized] float target;

        public override string Summary => $"BGM {seconds}s 到達";

        protected override void OnSetup(TreeScope scope) => target = seconds.Resolve(scope);
        public override bool Evaluate(BattleContext ctx) => ctx.BgmTime >= target;
    }
}
