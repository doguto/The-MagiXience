using System;
using System.Collections.Generic;
using Project.Scripts.Extensions;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.BehaviorTree
{
    [Serializable, NodeInfo("経路 (Trail Path) に沿ってこの敵を等速で動かす。移動が終わると完了", "Action/Move")]
    public sealed class MovePathNodeDefinition : ActionNodeDefinition
    {
        [SerializeField] BtObject<TrailPath> path;

        public override string Summary => TrailSummary.Of(path);

        public override INode Build(BuildScope scope) => new MovePathNode(path.Resolve(scope));

        sealed class MovePathNode : Node
        {
            readonly TrailPath path;
            TrailPolyline line;
            int elapsed;

            public MovePathNode(TrailPath path) => this.path = path;

            protected override void OnEnter(BattleContext ctx)
            {
                elapsed = 0;
                line = ctx.Self != null && path != null ? new TrailPolyline(ctx.Self.Body.Position, path) : null;
            }

            protected override NodeStatus OnTick(BattleContext ctx)
            {
                if (line == null) return NodeStatus.Failure;

                elapsed++;
                var p = line.PositionAtProgress(elapsed, path.Frames);
                var body = ctx.Self.Body;
                body.Position = new Vector3(p.x, p.y, body.Position.z);
                return elapsed >= path.Frames ? NodeStatus.Success : NodeStatus.Running;
            }
        }
    }

    // MovePath と並べて使う。この敵の位置ではなく経路から計算した位置に置くので、同じ経路なら移動とずれない
    [Serializable, NodeInfo("経路 (Trail Path) に沿って弾を等間隔に置き、移動完了 + Fire Delay 後に一斉に放つ", "Action/Attack")]
    public sealed class PlaceAlongPathNodeDefinition : ActionNodeDefinition
    {
        [SerializeField] BtObject<TrailPath> path;
        [SerializeField] BulletSpec bullet = new();
        [SerializeField, Tooltip("弾を置く間隔 (距離)")] BtFloat spacing = 0.5f;
        [SerializeField, Tooltip("経路の移動を終えてから放つまでのフレーム数")] BtInt fireDelayFrames = 0;
        [SerializeReference, SubclassSelector] RotationSource rotation = new InheritRotation();
        [SerializeField] SeType se;

        public override string Summary => $"{TrailSummary.Of(path)} / 間隔 {spacing} / 発射 +{fireDelayFrames.ToFrameString()} / {bullet}";

        public override INode Build(BuildScope scope) =>
            new PlaceNode(path.Resolve(scope), bullet.Resolve(scope), Mathf.Max(0.01f, spacing.Resolve(scope)), Mathf.Max(0, fireDelayFrames.Resolve(scope)), rotation, se);

        sealed class PlaceNode : Node
        {
            readonly TrailPath path;
            readonly BulletSpec bullet;
            readonly float spacing;
            readonly int fireDelay;
            readonly RotationSource rotation;
            readonly SeType se;
            readonly List<Placement> plan = new();
            TrailPolyline line;
            Quaternion rot;
            int elapsed;
            int spawned;

            struct Placement
            {
                public float Distance;
                public Vector2 Direction;
                public int ReleaseFrame;
            }

            public PlaceNode(TrailPath path, BulletSpec bullet, float spacing, int fireDelay, RotationSource rotation, SeType se)
            {
                this.path = path;
                this.bullet = bullet;
                this.spacing = spacing;
                this.fireDelay = fireDelay;
                this.rotation = rotation;
                this.se = se;
            }

            protected override void OnEnter(BattleContext ctx)
            {
                elapsed = 0;
                spawned = 0;
                plan.Clear();
                line = null;
                if (path == null) return;

                var e = new EmitContext(ctx, Vector2.zero);
                line = new TrailPolyline(e.Origin, path);
                rot = rotation?.GetRotation(e) ?? Quaternion.identity;
                BuildPlan();
                Emitter.PlaySe(ctx, se);
            }

            // 区間ごとに弾数を数え、区間内の並び順で扇の角度を決める
            void BuildPlan()
            {
                var segments = path.Segments;
                var count = Mathf.FloorToInt(line.Length / spacing + 1e-4f) + 1;
                var segmentOf = new int[count];
                var perSegment = new int[Mathf.Max(1, segments.Count)];
                for (var k = 0; k < count; k++)
                {
                    segmentOf[k] = segments.Count == 0 ? 0 : line.SegmentAt(k * spacing);
                    perSegment[segmentOf[k]]++;
                }

                var indexInSegment = new int[perSegment.Length];
                for (var k = 0; k < count; k++)
                {
                    var s = segmentOf[k];
                    var segment = segments.Count == 0 ? default : segments[s];
                    var baseDir = segment.direction.sqrMagnitude > 0f ? segment.direction.normalized : Vector2.left;
                    var n = perSegment[s];
                    var dir = n == 1 ? baseDir : ShotPattern.Rotate(ShotPattern.FanAngle(ShotPattern.Angle(baseDir), n, segment.spread, indexInSegment[s]));
                    indexInSegment[s]++;
                    plan.Add(new Placement
                    {
                        Distance = k * spacing,
                        Direction = dir,
                        ReleaseFrame = path.Frames + fireDelay + Mathf.Max(0, segment.extraFireDelayFrames),
                    });
                }
            }

            protected override NodeStatus OnTick(BattleContext ctx)
            {
                if (line == null) return NodeStatus.Failure;

                elapsed++;
                var reached = line.Length * Mathf.Clamp01(elapsed / (float)path.Frames);
                while (spawned < plan.Count && plan[spawned].Distance <= reached + 1e-4f)
                {
                    var placement = plan[spawned];
                    var pos = line.PositionAt(placement.Distance);
                    Emitter.Spawn(ctx, bullet, Vector2.zero, _ => pos, placement.Direction, rot, 0, Mathf.Max(0, placement.ReleaseFrame - elapsed));
                    spawned++;
                }
                return elapsed >= path.Frames && spawned >= plan.Count ? NodeStatus.Success : NodeStatus.Running;
            }
        }
    }

    static class TrailSummary
    {
        public static string Of(BtObject<TrailPath> path) =>
            !string.IsNullOrEmpty(path.param) ? "$" + path.param : path.value ? path.value.name : "(経路未設定)";
    }
}
