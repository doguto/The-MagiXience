using System;
using System.Collections.Generic;
using System.Linq;
using Project.Scenes.Battle.Scripts.Model.Entity;
using UniRx;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.BehaviorTree
{
    public enum HpSectionSwitch
    {
        [Tooltip("区切りを下回ったらすぐ次の段階へ")] Immediate,
        [Tooltip("区切りを下回った後、次の Phase の開始で次の段階へ")] NextPhase,
    }

    // 子が 1 つなら段階が進んでも同じ子を続け、段階は indexParam で子に伝える。子が複数なら段階ごとに子を切り替える
    [Serializable, NodeInfo("HP の区切りで段階を進める。区切りは HP ゲージにも表示する。子が複数なら段階ごとに子を切り替え (途中の子が先に終われば区切りまで待つ)、最後の子が終われば完了")]
    public sealed class HpSections : CompositeNode
    {
        [SerializeField] HpTarget target = HpTarget.Self;
        [SerializeField, Tooltip("区切りの HP (%)。大きい順")] List<float> breaks = new() { 50f };
        [SerializeField] HpSectionSwitch switchAt = HpSectionSwitch.Immediate;
        [SerializeField, ShowWhen(nameof(switchAt), HpSectionSwitch.NextPhase), Tooltip("区切りを下回ってから次の段階に入るまでの被ダメージ倍率")]
        float pendingDamageScale = 1f;
        [SerializeField, Tooltip("空でなければ、今の段階 (0 始まり) をこのパラメータに書き込む")] string indexParam;

        [NonSerialized] float[] ratios;
        [NonSerialized] EnemyEntityModel model;
        [NonSerialized] int stage;
        [NonSerialized] int running;
        [NonSerialized] bool holding;
        [NonSerialized] IDisposable phaseSubscription;

        public override string Summary =>
            "HP " + string.Join(" / ", breaks.Select(b => $"{b:0.#}%"))
            + (switchAt == HpSectionSwitch.NextPhase
                ? "  次の Phase から" + (Mathf.Approximately(pendingDamageScale, 1f) ? "" : $" (待機中の被ダメ ×{pendingDamageScale:0.##})")
                : "")
            + (string.IsNullOrEmpty(indexParam) ? "" : " → $" + indexParam);

        protected override void OnSetup(TreeScope scope) =>
            ratios = breaks.Select(b => Mathf.Clamp01(b / 100f)).OrderByDescending(r => r).ToArray();

        protected override void OnEnter(BattleContext ctx)
        {
            model = HpTargets.Resolve(ctx, target);
            (model as BossEntityModel)?.SetHpBreaks(ratios);
            SetStage(Reached());
            running = ChildIndex();
            holding = false;
            if (switchAt == HpSectionSwitch.NextPhase)
            {
                phaseSubscription = ctx.Services.OnPhaseStarted.Subscribe(_ => OnPhaseStarted());
            }
        }

        protected override NodeStatus OnTick(BattleContext ctx)
        {
            if (Children.Count == 0) return NodeStatus.Success;

            var reached = Reached();
            if (reached > stage)
            {
                if (switchAt == HpSectionSwitch.Immediate) SetStage(reached);
                else SetDamageScale(pendingDamageScale);
            }

            var want = ChildIndex();
            if (want != running)
            {
                Children[running].Abort(ctx);
                running = want;
                holding = false;
            }
            if (holding) return NodeStatus.Running;

            var status = Children[running].Tick(ctx);
            if (status != NodeStatus.Success) return status;
            if (running >= Children.Count - 1) return NodeStatus.Success;
            holding = true;
            return NodeStatus.Running;
        }

        protected override void OnAbort(BattleContext ctx)
        {
            if (Children.Count > 0) Children[running].Abort(ctx);
        }

        protected override void OnExit(BattleContext ctx)
        {
            phaseSubscription?.Dispose();
            phaseSubscription = null;
            SetDamageScale(1f);
        }

        // 子の Phase の開始 (= 子の Tick の途中) に呼ばれるため、子の切り替えは次の Tick で行う
        void OnPhaseStarted()
        {
            var reached = Reached();
            if (reached <= stage) return;
            SetStage(reached);
            SetDamageScale(1f);
        }

        int Reached()
        {
            if (model == null) return 0;
            var count = 0;
            foreach (var r in ratios)
            {
                if (model.CurrentHp.Value <= Mathf.CeilToInt(model.MaxHp * r)) count++;
            }
            return count;
        }

        int ChildIndex() => Mathf.Clamp(stage, 0, Mathf.Max(0, Children.Count - 1));

        void SetStage(int value)
        {
            stage = value;
            Scope?.Set(indexParam, value);
        }

        void SetDamageScale(float scale)
        {
            if (model is BossEntityModel boss) boss.DamageMultiplier = scale;
        }
    }
}
