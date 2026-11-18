using System;
using System.Collections.Generic;
using System.Linq;
using Project.Scenes.Battle.Scripts.Runtime;
using Project.Scenes.Battle.Scripts.Runtime.Motion;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.BehaviorTree
{
    [Serializable, NodeInfo("敵・障害物・ビームなどの Prefab を出現させる", "Action/Spawn")]
    public sealed class Spawn : ActionNode
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

        [NonSerialized] List<(Vector2 offset, Quaternion rotation)> slots;

        public override string Summary =>
            (prefab ? prefab.name : "(未設定)") + $" {layout?.Summary}" + (behavior ? $" / {behavior.name}" : "") + string.Concat(overrides.Select(OverrideSummary));

        static string OverrideSummary(TreeParameter p) => p.ObjectValue switch
        {
            MotionPreset motion => $" / {p.Name}: {MotionStep.Describe(motion.Steps)}",
            BehaviorTreeAsset tree => $" / {p.Name}: {tree.name}",
            _ => "",
        };

        protected override void OnSetup(TreeScope scope) => slots = new List<(Vector2, Quaternion)>();

        protected override NodeStatus OnTick(BattleContext ctx)
        {
            var spawner = ctx.Services.Spawner;
            if (spawner == null) return NodeStatus.Success;

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
                    Overrides = overrides,
                    Tracked = tracked,
                    BeamRange = beamRange,
                    BeamDuration = beamDuration,
                    BeamWidth = beamWidth,
                });
            }
            return NodeStatus.Success;
        }
    }
}
