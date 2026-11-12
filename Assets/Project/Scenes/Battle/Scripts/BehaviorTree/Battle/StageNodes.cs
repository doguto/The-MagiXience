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
    public sealed class NoActiveEnemiesConditionDefinition : ConditionDefinition
    {
        public override ICondition Build(BuildScope scope) =>
            new DelegateCondition(ctx => ctx.Services.Spawner == null || ctx.Services.Spawner.ActiveEnemyCount == 0);

        public override string Summary => "敵が全滅";
    }

    // 旧 BossHpThreshold / HpPercentThreshold。現在 HP ≦ ceil(最大 HP × 割合)
    [Serializable]
    public sealed class HpBelowConditionDefinition : ConditionDefinition
    {
        [SerializeField] HpTarget target = HpTarget.Self;
        [SerializeField, Range(0f, 100f)] float percent = 50f;

        public override ICondition Build(BuildScope scope) => new DelegateCondition(ctx =>
        {
            var model = HpTargets.Resolve(ctx, target);
            if (model == null) return true;
            return model.CurrentHp.Value <= Mathf.CeilToInt(model.MaxHp * percent / 100f);
        });

        public override string Summary => $"{(target == HpTarget.Boss ? "ボス" : "")}HP ≦ {percent:0.#}%";
    }

    // 旧「発狂 (Strong)」の判定。通常 HP を削り切ったら成立
    [Serializable]
    public sealed class NormalHpDepletedConditionDefinition : ConditionDefinition
    {
        [SerializeField] HpTarget target = HpTarget.Self;

        public override ICondition Build(BuildScope scope) => new DelegateCondition(ctx =>
            HpTargets.Resolve(ctx, target) is BossEntityModel boss && boss.ShouldUseStrongAttack);

        public override string Summary => "通常 HP を削り切った";
    }

    // 旧 BgmPositionExitCondition。BGM が指定サンプル位置に達するか、ループで先頭に戻ったら成立。
    // 開始時点で既に位置を過ぎていれば、ループで戻るのを待つ
    [Serializable]
    public sealed class BgmSamplesConditionDefinition : ConditionDefinition
    {
        [SerializeField, Min(0)] int thresholdSamples;

        public override ICondition Build(BuildScope scope)
        {
            var waitingForWrap = false;
            var last = 0;
            return new DelegateCondition(ctx =>
            {
                var source = ctx.Services.BgmSource;
                if (source == null || source.clip == null) return true;
                var now = source.timeSamples;
                var wrapped = now < last;
                last = now;
                if (wrapped) return true;
                return !waitingForWrap && now >= thresholdSamples;
            }, ctx =>
            {
                var source = ctx.Services.BgmSource;
                last = source != null ? source.timeSamples : 0;
                waitingForWrap = last >= thresholdSamples;
            });
        }

        public override string Summary => $"BGM {thresholdSamples} サンプル";
    }

    // ---------- 行動 ----------

    [Serializable, NodeInfo("ボスを発狂 (Strong) 状態にする", "Action/Boss")]
    public sealed class EnterStrongModeNodeDefinition : ActionNodeDefinition
    {
        [SerializeField] HpTarget target = HpTarget.Self;

        public override string Summary => "発狂開始";

        public override INode Build(BuildScope scope) => new ActionNode(ctx =>
        {
            (HpTargets.Resolve(ctx, target) as BossEntityModel)?.EnterStrongMode();
            return NodeStatus.Success;
        });
    }

    [Serializable, NodeInfo("画面の弾・警告線・ビームを消す。敵も消せる", "Action/Stage")]
    public sealed class ClearBulletsNodeDefinition : ActionNodeDefinition
    {
        [SerializeField] bool clearEnemies;

        public override string Summary => clearEnemies ? "弾と敵を消す" : "弾を消す";

        public override INode Build(BuildScope scope) => new ActionNode(ctx =>
        {
            ctx.Services.Bullets.Clear();
            ctx.Services.Clearer?.ClearAllBullets();
            if (clearEnemies) ctx.Services.Clearer?.ClearAllEnemies();
            return NodeStatus.Success;
        });
    }

    // 子の実行中だけ風を吹かせ、終わったら直前の風に戻す。バトル開始時は無風
    [Serializable, NodeInfo("子の実行中だけ風を吹かせる。終わると直前の風に戻る", "Decorator")]
    public sealed class WithWindNodeDefinition : DecoratorNodeDefinition
    {
        [SerializeField] BattleWindSettings wind = new() { enabled = true };

        public override string Summary => wind.intervals.Count == 0 ? "風 (未設定)"
            : string.Join(" → ", wind.intervals.Select(i => $"{i.direction} {i.seconds:0.#}s"));

        public override INode Build(BuildScope scope) => new WithWindNode(BuildChild(scope), wind);

        sealed class WithWindNode : Node
        {
            readonly INode child;
            readonly BattleWindSettings wind;
            BattleWindSettings previous;

            public WithWindNode(INode child, BattleWindSettings wind)
            {
                this.child = child;
                this.wind = wind;
            }

            WindService service;

            protected override void OnEnter(BattleContext ctx)
            {
                service = ctx.Stage?.Get<WindService>();
                if (service == null)
                {
                    ctx.Stage?.WarnMissing<WindService>("WithWind");
                    return;
                }
                previous = service.Current;
                service.Apply(wind);
            }

            protected override NodeStatus OnTick(BattleContext ctx) => child.Tick(ctx);
            protected override void OnAbort(BattleContext ctx) => child.Abort(ctx);

            protected override void OnExit(BattleContext ctx)
            {
                service?.Apply(previous);
                service = null;
                previous = null;
            }
        }
    }

    // 旧 BossWindCue。合図の演出 (IWindCueReceiver を持つ敵のみ) のあと風向きを切り替える
    [Serializable, NodeInfo("合図のあと風向きを切り替える", "Action/Boss")]
    public sealed class BossWindCueNodeDefinition : ActionNodeDefinition
    {
        [SerializeField] BossWindCueSettings cue = new() { enabled = true };

        public override string Summary => $"風の合図 → {cue.direction} ({cue.cueSeconds:0.##}s)";

        public override INode Build(BuildScope scope) => new ActionNode(ctx =>
        {
            var wind = ctx.Stage?.Get<WindService>();
            if (wind == null)
            {
                ctx.Stage?.WarnMissing<WindService>("BossWindCue");
                return NodeStatus.Success;
            }
            wind.Apply(cue.CreateWind(wind.Direction));
            if (ctx.Self != null && ctx.Self.TryGetComponent<IWindCueReceiver>(out var receiver)) receiver.PlayCue(cue);
            return NodeStatus.Success;
        });
    }

    [Serializable, NodeInfo("フェーズの区切りを通知する (進行ゲージなどが使う)", "Action/Stage")]
    public sealed class PhaseMarkerNodeDefinition : ActionNodeDefinition
    {
        [SerializeField] string phaseId;

        public override string Summary => phaseId;

        public override INode Build(BuildScope scope) => new ActionNode(ctx =>
        {
            ctx.Services.NotifyPhase(phaseId);
            return NodeStatus.Success;
        });
    }

    // 旧 Control トラック。子の実行中だけ Prefab を出しておく
    [Serializable, NodeInfo("子の実行中だけ Prefab を出しておく (障害物・演出など)", "Decorator")]
    public sealed class WithPrefabNodeDefinition : DecoratorNodeDefinition
    {
        [SerializeField] GameObject prefab;
        [SerializeField] Vector3 position;

        public override string Summary => prefab ? prefab.name : "(未設定)";

        public override INode Build(BuildScope scope) => new WithPrefabNode(BuildChild(scope), prefab, position);

        sealed class WithPrefabNode : Node
        {
            readonly INode child;
            readonly GameObject prefab;
            readonly Vector3 position;
            GameObject instance;

            public WithPrefabNode(INode child, GameObject prefab, Vector3 position)
            {
                this.child = child;
                this.prefab = prefab;
                this.position = position;
            }

            protected override void OnEnter(BattleContext ctx)
            {
                if (prefab != null) instance = UnityEngine.Object.Instantiate(prefab, position, prefab.transform.rotation);
            }

            protected override NodeStatus OnTick(BattleContext ctx) => child.Tick(ctx);
            protected override void OnAbort(BattleContext ctx) => child.Abort(ctx);

            protected override void OnExit(BattleContext ctx)
            {
                if (instance != null) UnityEngine.Object.Destroy(instance);
                instance = null;
            }
        }
    }
}
