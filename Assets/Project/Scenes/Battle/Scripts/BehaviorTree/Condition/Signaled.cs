using System;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.BehaviorTree
{
    // このフレームに自分へ合図が届いたか。届いたフレームに判定されなかった合図は受け取れない
    [Serializable]
    public sealed class Signaled : Condition
    {
        [SerializeField] string key;

        public override string Summary => string.IsNullOrEmpty(key) ? "(合図未設定)" : $"合図 {key} が届いた";

        public override bool Evaluate(BattleContext ctx) => ctx.Self != null && ctx.Self.Signals.Contains(key, ctx.Frame);
    }
}
