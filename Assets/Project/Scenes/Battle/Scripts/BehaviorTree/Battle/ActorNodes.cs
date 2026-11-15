using System;
using System.Collections.Generic;
using Project.Scenes.Battle.Scripts.Runtime;
using Project.Scenes.Battle.Scripts.Runtime.Motion;
using Project.Scripts.Extensions;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.BehaviorTree
{
    [Serializable, NodeInfo("この敵を動かす。移動が終わると完了 (無限の移動は完了しない)", "Action/Move")]
    public sealed class MoveNodeDefinition : ActionNodeDefinition
    {
        [SerializeField, Tooltip("指定するとインラインより優先")] MotionPreset preset;
        [SerializeReference, SubclassSelector] List<MotionStep> steps = new();

        public IReadOnlyList<MotionStep> Steps => preset != null ? preset.Steps : steps;

        public override string Summary
        {
            get
            {
                if (preset != null) return preset.name;
                if (steps.Count == 1) return steps[0]?.Summary;
                return steps.Count == 0 ? "(移動なし)" : $"{steps.Count} ステップ";
            }
        }

        public override INode Build(BuildScope scope) => new MoveNode(this);

        sealed class MoveNode : Node
        {
            readonly MoveNodeDefinition def;
            MotionSequence sequence;

            public MoveNode(MoveNodeDefinition def) => this.def = def;

            protected override void OnEnter(BattleContext ctx)
            {
                if (ctx.Self == null) return;
                sequence = new MotionSequence(def.Steps, ctx.Self.Body, new MotionLaunch { AttackTime = ctx.Self.AttackTime });
            }

            protected override NodeStatus OnTick(BattleContext ctx)
            {
                if (sequence == null) return NodeStatus.Failure;
                sequence.Tick(BattleWorld.DeltaTime);
                return sequence.IsFinished ? NodeStatus.Success : NodeStatus.Running;
            }

            protected override void OnExit(BattleContext ctx)
            {
                sequence?.Stop();
                sequence = null;
            }
        }
    }

    [Serializable, NodeInfo("この敵の移動を差し替える。移動はツリーと独立して流れ続け、このノードはすぐ完了する", "Action/Move")]
    public sealed class SetMovementNodeDefinition : ActionNodeDefinition
    {
        [SerializeField, Tooltip("指定するとインラインより優先")] MotionPreset preset;
        [SerializeField, Tooltip("ツリーの Object パラメータ名。指定するとその値の MotionPreset を使う")] string presetParam;
        [SerializeReference, SubclassSelector] List<MotionStep> steps = new();
        [SerializeField, Tooltip("同じ移動が再生中ならやり直さない (フェーズをまたいで続く移動)")] bool continueIfSame;

        public IReadOnlyList<MotionStep> Steps => preset != null ? preset.Steps : steps;

        public override string Summary =>
            !string.IsNullOrEmpty(presetParam) ? "$" + presetParam
            : preset != null ? preset.name : steps.Count == 0 ? "移動を止める" : steps.Count == 1 ? steps[0]?.Summary : $"{steps.Count} ステップ";

        public override INode Build(BuildScope scope)
        {
            var resolved = ParamObjects.Resolve(scope, presetParam, preset);
            IReadOnlyList<MotionStep> resolvedSteps = resolved != null ? resolved.Steps : string.IsNullOrEmpty(presetParam) ? steps : null;
            return new ActionNode(ctx =>
            {
                ctx.Self?.SetMovement(resolvedSteps, continueIfSame);
                return NodeStatus.Success;
            });
        }
    }

    [Serializable, NodeInfo("この敵が倒されたときに 1 回だけ実行するツリーを設定する", "Action/Actor")]
    public sealed class SetDeathBehaviorNodeDefinition : ActionNodeDefinition
    {
        [SerializeField] BehaviorTreeAsset tree;
        [SerializeField, Tooltip("ツリーの Object パラメータ名。指定するとその値のツリーを使う (値が空なら撃破時の行動なし)")] string treeParam;

        public override string Summary =>
            !string.IsNullOrEmpty(treeParam) ? $"撃破時: ${treeParam}" : tree ? $"撃破時: {tree.name}" : "撃破時の行動なし";

        public override INode Build(BuildScope scope)
        {
            var resolved = ParamObjects.Resolve(scope, treeParam, tree);
            return new ActionNode(ctx =>
            {
                ctx.Self?.SetDeathBehavior(resolved);
                return NodeStatus.Success;
            });
        }
    }

    [Serializable, NodeInfo("この敵の無敵を切り替える", "Action/Actor")]
    public sealed class SetInvincibleNodeDefinition : ActionNodeDefinition
    {
        [SerializeField] bool invincible = true;

        public override string Summary => invincible ? "無敵にする" : "無敵を解除";

        public override INode Build(BuildScope scope) => new ActionNode(ctx =>
        {
            ctx.Self?.Model?.SetInvincible(invincible);
            return NodeStatus.Success;
        });
    }

    [Serializable, NodeInfo("この敵を消す (撃破扱いにしない)", "Action/Actor")]
    public sealed class DespawnNodeDefinition : ActionNodeDefinition
    {
        public override string Summary => "退場";

        public override INode Build(BuildScope scope) => new ActionNode(ctx =>
        {
            if (ctx.Self != null)
            {
                ctx.World.CancelScheduled(ctx.Self);
                ActorDespawn.Despawn(ctx.Self.gameObject);
            }
            return NodeStatus.Success;
        });
    }

    [Serializable, NodeInfo("効果音を鳴らす", "Action")]
    public sealed class PlaySeNodeDefinition : ActionNodeDefinition
    {
        [SerializeField] SeType se;

        public override string Summary => se.ToString();

        public override INode Build(BuildScope scope) => new ActionNode(ctx =>
        {
            Emitter.PlaySe(ctx, se);
            return NodeStatus.Success;
        });
    }

    // 敵などを出現させる。撃つ敵がいればその位置から、いなければワールド座標
    [Serializable]
    public abstract class SpawnLayout
    {
        public abstract void Build(Vector2 baseDirection, List<(Vector2 offset, Quaternion rotation)> slots);
        public virtual string Summary => GetType().Name.Replace("Layout", "");
    }

    [Serializable]
    public sealed class SingleLayout : SpawnLayout
    {
        [SerializeField] Vector2 offset;

        public override void Build(Vector2 baseDirection, List<(Vector2, Quaternion)> slots) => slots.Add((offset, Quaternion.identity));
        public override string Summary => offset == Vector2.zero ? "1体" : $"1体 {offset}";
    }

    // 旧 LineEnemySpawnAttackSignal。中心から等間隔に並べる
    [Serializable]
    public sealed class LineLayout : SpawnLayout
    {
        [SerializeField, Min(1)] int count = 5;
        [SerializeField] float spacing = 1f;
        [SerializeField] Vector2 lineAxis = Vector2.up;
        [SerializeField] Vector2 centerOffset;

        public override void Build(Vector2 baseDirection, List<(Vector2, Quaternion)> slots)
        {
            var axis = lineAxis.sqrMagnitude > 0f ? lineAxis.normalized : Vector2.up;
            for (var i = 0; i < count; i++) slots.Add((centerOffset + axis * ((i - (count - 1) / 2f) * spacing), Quaternion.identity));
        }

        public override string Summary => $"{count}体 一列";
    }

    // 旧 NWayEnemySpawnAttackSignal。扇状に並べ、各体を扇の角度だけ回す
    [Serializable]
    public sealed class FanLayout : SpawnLayout
    {
        [SerializeField, Min(1)] int wayCount = 3;
        [SerializeField] float spreadAngle = 60f;
        [SerializeField] Vector2 offset;

        public override void Build(Vector2 baseDirection, List<(Vector2, Quaternion)> slots)
        {
            for (var i = 0; i < wayCount; i++)
            {
                var delta = wayCount == 1 ? 0f
                    : wayCount % 2 == 1 ? -spreadAngle * (wayCount - 1) / 2f + spreadAngle * i
                    : -spreadAngle / 2f - spreadAngle * (wayCount / 2 - 1) + spreadAngle * i;
                var rot = Quaternion.Euler(0f, 0f, delta);
                slots.Add(((Vector2)(rot * offset), rot));
            }
        }

        public override string Summary => $"{wayCount}体 扇";
    }

    [Serializable, NodeInfo("敵・障害物・ビームなどの Prefab を出現させる", "Action/Spawn")]
    public sealed class SpawnNodeDefinition : ActionNodeDefinition
    {
        [SerializeField] GameObject prefab;
        [SerializeField, Tooltip("この Prefab を動かすツリー。未指定ならツリーを付けない (自前で動く Prefab 用)")] BehaviorTreeAsset behavior;
        [SerializeField] List<TreeParameter> overrides = new();
        [SerializeField, Tooltip("撃つ敵がいなければワールド座標、いればその位置からの相対")] Vector2 position;
        [SerializeField, Tooltip("撃つ敵がいてもワールド座標で出す")] bool worldSpace;
        [SerializeReference, SubclassSelector] SpawnLayout layout = new SingleLayout();
        [SerializeReference, SubclassSelector] RotationSource rotation;
        [SerializeReference, SubclassSelector] DirectionSource direction;
        [SerializeField, Tooltip("「敵の全滅」の判定対象にする")] bool tracked = true;
        [SerializeField, Tooltip("ビーム/警告線の長さ。0 以下は Prefab の値")] float beamRange;
        [SerializeField, Tooltip("ビーム/警告線の表示時間 (秒)。0 以下は Prefab の値")] float beamDuration;
        [SerializeField] float beamWidth;

        public override string Summary => (prefab ? prefab.name : "(未設定)") + $" {layout?.Summary}" + (behavior ? $" / {behavior.name}" : "");

        public override INode Build(BuildScope scope)
        {
            var slots = new List<(Vector2 offset, Quaternion rotation)>();
            var resolvedOverrides = overrides;
            return new ActionNode(ctx =>
            {
                Spawn(ctx, slots, resolvedOverrides);
                return NodeStatus.Success;
            });
        }

        void Spawn(BattleContext ctx, List<(Vector2 offset, Quaternion rotation)> slots, IReadOnlyList<TreeParameter> resolvedOverrides)
        {
            var spawner = ctx.Services.Spawner;
            if (spawner == null) return;

            var e = worldSpace ? new EmitContext(new BattleContext(ctx.World), position) : new EmitContext(ctx, position);
            var dir = direction?.GetDirection(e) ?? Vector2.left;
            var baseRot = rotation?.GetRotation(e) ?? Quaternion.identity;
            var z = ctx.Self != null ? ctx.Self.Body.Position.z : 0f;

            slots.Clear();
            (layout ?? new SingleLayout()).Build(dir, slots);
            foreach (var (offset, rot) in slots)
            {
                var p = e.Origin + offset;
                spawner.Spawn(new ActorSpawn
                {
                    Prefab = prefab,
                    Position = new Vector3(p.x, p.y, z),
                    Rotation = rot * baseRot,
                    Behavior = behavior,
                    Overrides = resolvedOverrides,
                    Tracked = tracked,
                    BeamRange = beamRange,
                    BeamDuration = beamDuration,
                    BeamWidth = beamWidth,
                });
            }
        }
    }
}
