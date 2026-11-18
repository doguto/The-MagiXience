using System;
using System.Collections.Generic;
using Project.Scripts.Extensions;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.BehaviorTree
{
    // MovePath と並べて使う。この敵の位置ではなく経路から計算した位置に置くので、同じ経路なら移動とずれない
    [Serializable, NodeInfo("経路 (Trail Path) に沿って弾を等間隔に置き、移動完了 + Fire Delay 後に一斉に放つ", "Action/Attack")]
    public sealed class PlaceAlongPath : ActionNode
    {
        [SerializeField] BtObject<TrailPath> path;
        [SerializeField] BulletConfig bullet = new();
        [SerializeField, Tooltip("弾を置く間隔 (距離)")] BtFloat spacing = 0.5f;
        [SerializeField, Tooltip("経路の移動を終えてから放つまでのフレーム数")] BtInt fireDelayFrames = 0;
        [SerializeReference, SubclassSelector] RotationSource rotation = new InheritRotation();
        [SerializeField] SeType se;

        [NonSerialized] TrailPath resolvedPath;
        [NonSerialized] BulletConfig resolvedBullet;
        [NonSerialized] float resolvedSpacing;
        [NonSerialized] int fireDelay;
        [NonSerialized] List<Placement> plan;
        [NonSerialized] TrailPolyline line;
        [NonSerialized] Quaternion rot;
        [NonSerialized] int elapsed;
        [NonSerialized] int spawned;

        struct Placement
        {
            public float Distance;
            public Vector2 Direction;
            public int ReleaseFrame;
        }

        public override string Summary => $"{TrailSummary.Of(path)} / 間隔 {spacing} / 発射 +{fireDelayFrames.ToFrameString()} / {bullet}";

        protected override void OnSetup(TreeScope scope)
        {
            resolvedPath = path.Resolve(scope);
            resolvedBullet = bullet.Resolve(scope);
            resolvedSpacing = Mathf.Max(0.01f, spacing.Resolve(scope));
            fireDelay = Mathf.Max(0, fireDelayFrames.Resolve(scope));
            plan = new List<Placement>();
        }

        protected override void OnEnter(BattleContext ctx)
        {
            elapsed = 0;
            spawned = 0;
            plan.Clear();
            line = null;
            if (resolvedPath == null) return;

            var e = new EmitContext(ctx, Vector2.zero, Scope);
            line = new TrailPolyline(e.Origin, resolvedPath);
            rot = rotation?.GetRotation(e) ?? Quaternion.identity;
            BuildPlan();
            Emitter.PlaySe(ctx, se);
        }

        // 区間ごとに弾数を数え、区間内の並び順で扇の角度を決める
        void BuildPlan()
        {
            var segments = resolvedPath.Segments;
            var count = Mathf.FloorToInt(line.Length / resolvedSpacing + 1e-4f) + 1;
            var segmentOf = new int[count];
            var perSegment = new int[Mathf.Max(1, segments.Count)];
            for (var k = 0; k < count; k++)
            {
                segmentOf[k] = segments.Count == 0 ? 0 : line.SegmentAt(k * resolvedSpacing);
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
                    Distance = k * resolvedSpacing,
                    Direction = dir,
                    ReleaseFrame = resolvedPath.Frames + fireDelay + Mathf.Max(0, segment.extraFireDelayFrames),
                });
            }
        }

        protected override NodeStatus OnTick(BattleContext ctx)
        {
            if (line == null) return NodeStatus.Failure;

            elapsed++;
            var reached = line.Length * Mathf.Clamp01(elapsed / (float)resolvedPath.Frames);
            while (spawned < plan.Count && plan[spawned].Distance <= reached + 1e-4f)
            {
                var placement = plan[spawned];
                var pos = line.PositionAt(placement.Distance);
                Emitter.Spawn(ctx, resolvedBullet, Vector2.zero, _ => pos, placement.Direction, rot, 0, Mathf.Max(0, placement.ReleaseFrame - elapsed));
                spawned++;
            }
            return elapsed >= resolvedPath.Frames && spawned >= plan.Count ? NodeStatus.Success : NodeStatus.Running;
        }
    }
}
