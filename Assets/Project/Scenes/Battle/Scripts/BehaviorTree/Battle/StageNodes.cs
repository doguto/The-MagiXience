using System;
using System.Linq;
using Project.Scenes.Battle.Scripts.Model;
using Project.Scenes.Battle.Scripts.Model.Entity;
using Project.Scenes.Battle.Scripts.Runtime;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.BehaviorTree
{
    public enum HpTarget
    {
        [Tooltip("このツリーを持つ敵")] Self,
        [Tooltip("ボス")] Boss,
    }

    static class HpTargets
    {
        public static EnemyEntityModel Resolve(BattleContext ctx, HpTarget target) =>
            target == HpTarget.Self ? ctx.Self?.Model : ctx.Services.Boss?.Model;
    }

    // ---------- 条件 ----------

    [Serializable]
    public sealed class NoActiveEnemies : Condition
    {
        public override string Summary => "敵が全滅";
        public override bool Evaluate(BattleContext ctx) => ctx.Services.Spawner == null || ctx.Services.Spawner.ActiveEnemyCount == 0;
    }

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

    // 旧「発狂 (Strong)」の判定。通常 HP を削り切ったら成立
    [Serializable]
    public sealed class NormalHpDepleted : Condition
    {
        [SerializeField] HpTarget target = HpTarget.Self;

        public override string Summary => "通常 HP を削り切った";
        public override bool Evaluate(BattleContext ctx) => HpTargets.Resolve(ctx, target) is BossEntityModel boss && boss.ShouldUseStrongAttack;
    }

    // 旧 BgmPositionExitCondition。BGM が指定サンプル位置に達するか、ループで先頭に戻ったら成立。
    // 開始時点で既に位置を過ぎていれば、ループで戻るのを待つ
    [Serializable]
    public sealed class BgmSamples : Condition
    {
        [SerializeField, Min(0)] int thresholdSamples;

        [NonSerialized] bool waitingForWrap;
        [NonSerialized] int last;

        public override string Summary => $"BGM {thresholdSamples} サンプル";

        public override void Reset(BattleContext ctx)
        {
            var source = ctx.Services.BgmSource;
            last = source != null ? source.timeSamples : 0;
            waitingForWrap = last >= thresholdSamples;
        }

        public override bool Evaluate(BattleContext ctx)
        {
            var source = ctx.Services.BgmSource;
            if (source == null || source.clip == null) return true;
            var now = source.timeSamples;
            var wrapped = now < last;
            last = now;
            if (wrapped) return true;
            return !waitingForWrap && now >= thresholdSamples;
        }
    }

    // ---------- 行動 ----------

    [Serializable, NodeInfo("ボスを発狂 (Strong) 状態にする", "Action/Boss")]
    public sealed class EnterStrongMode : ActionNode
    {
        [SerializeField] HpTarget target = HpTarget.Self;

        public override string Summary => "発狂開始";

        protected override NodeStatus OnTick(BattleContext ctx)
        {
            (HpTargets.Resolve(ctx, target) as BossEntityModel)?.EnterStrongMode();
            return NodeStatus.Success;
        }
    }

    [Serializable, NodeInfo("画面の弾・警告線・ビームを消す。敵も消せる", "Action/Stage")]
    public sealed class ClearBullets : ActionNode
    {
        [SerializeField] bool clearEnemies;

        public override string Summary => clearEnemies ? "弾と敵を消す" : "弾を消す";

        protected override NodeStatus OnTick(BattleContext ctx)
        {
            ctx.Services.Bullets.Clear();
            ctx.Services.Clearer?.ClearAllBullets();
            if (clearEnemies) ctx.Services.Clearer?.ClearAllEnemies();
            return NodeStatus.Success;
        }
    }

    // バトル開始時は無風
    [Serializable, NodeInfo("子の実行中だけ風を吹かせる。終わると直前の風に戻る", "Decorator")]
    public sealed class WithWind : DecoratorNode
    {
        [SerializeField] BattleWindSettings wind = new() { enabled = true };

        [NonSerialized] WindService service;
        [NonSerialized] BattleWindSettings previous;

        public override string Summary => wind.intervals.Count == 0 ? "風 (未設定)"
            : string.Join(" → ", wind.intervals.Select(i => $"{i.direction} {i.seconds:0.#}s"));

        protected override void OnEnter(BattleContext ctx)
        {
            service = ctx.Stage?.Get<WindService>();
            if (service == null)
            {
                ctx.Stage?.WarnMissing<WindService>(nameof(WithWind));
                return;
            }
            previous = service.Current;
            service.Apply(wind);
        }

        protected override NodeStatus OnTick(BattleContext ctx) => Child.Tick(ctx);
        protected override void OnAbort(BattleContext ctx) => Child.Abort(ctx);

        protected override void OnExit(BattleContext ctx)
        {
            service?.Apply(previous);
            service = null;
            previous = null;
        }
    }

    // 旧 BossWindCue。合図の演出 (IWindCueReceiver を持つ敵のみ) のあと風向きを切り替える
    [Serializable, NodeInfo("合図のあと風向きを切り替える", "Action/Boss")]
    public sealed class BossWindCue : ActionNode
    {
        [SerializeField] BossWindCueSettings cue = new() { enabled = true };

        public override string Summary => $"風の合図 → {cue.direction} ({cue.cueSeconds:0.##}s)";

        protected override NodeStatus OnTick(BattleContext ctx)
        {
            var wind = ctx.Stage?.Get<WindService>();
            if (wind == null)
            {
                ctx.Stage?.WarnMissing<WindService>(nameof(BossWindCue));
                return NodeStatus.Success;
            }
            wind.Apply(cue.CreateWind(wind.Direction));
            if (ctx.Self != null && ctx.Self.TryGetComponent<IWindCueReceiver>(out var receiver)) receiver.PlayCue(cue);
            return NodeStatus.Success;
        }
    }

    [Serializable, NodeInfo("フェーズの区切りを通知する (進行ゲージなどが使う)", "Action/Stage")]
    public sealed class PhaseMarker : ActionNode
    {
        [SerializeField] string phaseId;

        public override string Summary => phaseId;

        protected override NodeStatus OnTick(BattleContext ctx)
        {
            ctx.Services.NotifyPhase(phaseId);
            return NodeStatus.Success;
        }
    }

    // 旧 Control トラック
    [Serializable, NodeInfo("子の実行中だけ Prefab を出しておく (障害物・演出など)", "Decorator")]
    public sealed class WithPrefab : DecoratorNode
    {
        [SerializeField] GameObject prefab;
        [SerializeField] Vector3 position;

        [NonSerialized] GameObject instance;

        public override string Summary => prefab ? prefab.name : "(未設定)";

        protected override void OnEnter(BattleContext ctx)
        {
            if (prefab != null) instance = UnityEngine.Object.Instantiate(prefab, position, prefab.transform.rotation);
        }

        protected override NodeStatus OnTick(BattleContext ctx) => Child.Tick(ctx);
        protected override void OnAbort(BattleContext ctx) => Child.Abort(ctx);

        protected override void OnExit(BattleContext ctx)
        {
            if (instance != null) UnityEngine.Object.Destroy(instance);
            instance = null;
        }
    }
}
