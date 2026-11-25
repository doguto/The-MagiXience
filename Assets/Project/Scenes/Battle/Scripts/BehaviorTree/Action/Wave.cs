using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.BehaviorTree
{
    // ---------- 道中 ----------

    [Serializable]
    public sealed class WaveEntry
    {
        [Min(0), Tooltip("Wave の開始から数えたフレーム")] public int frame;
        [SerializeReference] public Spawn spawn = new();
    }

    [Serializable, NodeInfo("出現一覧を時刻どおりに出し、全部出したあと終了条件が成立したら完了する", "Flow")]
    public sealed class Wave : ActionNode
    {
        [SerializeField] List<WaveEntry> entries = new();
        [SerializeReference, SubclassSelector, Tooltip("全部出したあとに待つ条件。未設定なら最後の出現で完了")] Condition until;

        [NonSerialized] WaveEntry[] ordered;
        [NonSerialized] int elapsed;
        [NonSerialized] int next;

        public List<WaveEntry> Entries => entries;

        public Condition Until
        {
            get => until;
            set => until = value;
        }

        public override string Summary
        {
            get
            {
                var last = entries.Count == 0 ? 0 : entries.Max(e => e.frame);
                return $"{entries.Count} 体 / {last}f まで" + (until != null ? " → " + until.Summary : "");
            }
        }

        protected override void OnSetup(TreeScope scope)
        {
            ordered = entries.Where(e => e?.spawn != null).OrderBy(e => e.frame).ToArray();
            foreach (var entry in ordered) entry.spawn.Setup(scope);
            until?.Setup(scope);
        }

        protected override void OnEnter(BattleContext ctx)
        {
            elapsed = 0;
            next = 0;
            until?.Reset(ctx);
        }

        protected override NodeStatus OnTick(BattleContext ctx)
        {
            while (next < ordered.Length && ordered[next].frame <= elapsed)
            {
                ordered[next].spawn.Tick(ctx);
                next++;
            }
            elapsed++;
            if (next < ordered.Length) return NodeStatus.Running;
            return until == null || until.Evaluate(ctx) ? NodeStatus.Success : NodeStatus.Running;
        }
    }
}
