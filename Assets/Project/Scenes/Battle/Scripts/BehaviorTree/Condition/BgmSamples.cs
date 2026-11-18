using System;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.BehaviorTree
{
    // 旧 BgmPositionExitCondition。BGM が指定サンプル位置に達するか、ループで先頭に戻ったら成立。
    // 開始時点で既に位置を過ぎていれば、ループで戻るのを待つ
    [Serializable]
    public sealed class BgmSamples : Condition
    {
        [SerializeField] BtInt thresholdSamples;

        [NonSerialized] int threshold;
        [NonSerialized] bool waitingForWrap;
        [NonSerialized] int last;

        public override string Summary => $"BGM {thresholdSamples} サンプル";

        protected override void OnSetup(TreeScope scope) => threshold = Mathf.Max(0, thresholdSamples.Resolve(scope));

        public override void Reset(BattleContext ctx)
        {
            var source = ctx.Services.BgmSource;
            last = source != null ? source.timeSamples : 0;
            waitingForWrap = last >= threshold;
        }

        public override bool Evaluate(BattleContext ctx)
        {
            var source = ctx.Services.BgmSource;
            if (source == null || source.clip == null) return true;
            var now = source.timeSamples;
            var wrapped = now < last;
            last = now;
            if (wrapped) return true;
            return !waitingForWrap && now >= threshold;
        }
    }
}
