using System;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.BehaviorTree
{
    [Serializable]
    public sealed class FrameElapsed : Condition
    {
        [SerializeField, Tooltip("Reset からの経過フレーム数")] BtInt frames;

        [NonSerialized] int target;
        [NonSerialized] int startFrame;

        public override string Summary => "経過 " + frames.ToFrameString();

        protected override void OnSetup(TreeScope scope) => target = frames.Resolve(scope);
        public override void Reset(BattleContext ctx) => startFrame = ctx.Frame;
        public override bool Evaluate(BattleContext ctx) => ctx.Frame - startFrame >= target;
        public override float? Progress(BattleContext ctx) => target <= 0 ? 1f : Mathf.Clamp01((ctx.Frame - startFrame) / (float)target);
    }
}
