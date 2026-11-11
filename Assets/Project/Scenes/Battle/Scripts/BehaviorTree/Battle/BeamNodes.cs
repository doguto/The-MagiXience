using System;
using System.Collections.Generic;
using Project.Scenes.Battle.Scripts.Runtime;
using Project.Scenes.Battle.Scripts.Runtime.Motion;
using Project.Scripts.Extensions;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.BehaviorTree
{
    [Serializable]
    public struct BeamSegment
    {
        public Vector2 start;
        public Vector2 end;

        public Vector2 Direction => (end - start).sqrMagnitude > 0f ? (end - start).normalized : Vector2.left;
        public float Length => Vector2.Distance(start, end);
        public Quaternion Rotation => Quaternion.Euler(0f, 0f, Mathf.Atan2(Direction.y, Direction.x) * Mathf.Rad2Deg);
    }

    // 旧 BeamAttackSignal。警告線のあと、線分の始点から終点まで弾を連射する
    [Serializable, NodeInfo("警告線を出したあと、線分に沿って弾を連射するビーム", "Action/Attack")]
    public sealed class BulletBeamNodeDefinition : ActionNodeDefinition
    {
        [SerializeField, Tooltip("ワールド座標")] BeamSegment line = new() { start = new Vector2(5f, 0f), end = new Vector2(-9f, 0f) };
        [SerializeField] BulletSpec bullet = new();
        [SerializeReference, SubclassSelector, Tooltip("1 回の連射で撃つ形。方向は線分の向きになる")] ShotPattern pattern = new SingleShot();
        [SerializeField, Min(1)] int shotCount = 36;
        [SerializeField, Min(1)] int shotIntervalFrames = 5;
        [SerializeField] bool showWarning = true;
        [SerializeField] GameObject warningPrefab;
        [SerializeField, Min(0), Tooltip("警告線の表示フレーム数。警告線を出さない場合も連射開始はこの分だけ待つ")] int warningFrames = 60;
        [SerializeField, Min(0f)] float warningWidth;
        [SerializeField] SeType se;

        public override string Summary => $"弾ビーム {shotCount}発 / 警告 {warningFrames}f";

        public override INode Build(BuildScope scope)
        {
            var shots = new List<Shot>();
            return new ActionNode(ctx =>
            {
                Fire(ctx, shots);
                return NodeStatus.Success;
            });
        }

        void Fire(BattleContext ctx, List<Shot> shots)
        {
            var world = ctx.World;
            var self = ctx.Self;
            var segment = line;

            if (showWarning && warningPrefab != null)
            {
                ctx.Services.Spawner?.Spawn(new ActorSpawn
                {
                    Prefab = warningPrefab,
                    Position = segment.start,
                    Rotation = segment.Rotation,
                    BeamRange = segment.Length,
                    BeamDuration = warningFrames * BattleWorld.DeltaTime,
                    BeamWidth = warningWidth,
                });
            }

            shots.Clear();
            (pattern ?? new SingleShot()).Build(segment.Direction, shots);
            var snapshot = shots.ToArray();

            for (var i = 0; i < shotCount; i++)
            {
                var delay = warningFrames + i * shotIntervalFrames;
                world.Schedule(delay, () =>
                {
                    Emitter.PlaySe(ctx, se);
                    foreach (var shot in snapshot)
                    {
                        var offset = shot.Offset;
                        Emitter.Spawn(ctx, bullet, Vector2.zero, _ => segment.start + offset, shot.Direction, segment.Rotation,
                            shot.SpawnDelayFrames, shot.StartDelayFrames, shot.Motion, segment.Length);
                    }
                }, self);
            }
        }
    }

    // 旧 SpriteBeamAttackSignal。警告線のあと、1 枚絵のビーム本体を出す
    [Serializable, NodeInfo("警告線のあと、画像のビーム本体を出す", "Action/Attack")]
    public sealed class SpriteBeamNodeDefinition : ActionNodeDefinition
    {
        [SerializeField] BeamSegment line = new() { start = new Vector2(5f, 0f), end = new Vector2(-9f, 0f) };
        [SerializeField, Tooltip("ON: 線分を撃つ敵の位置からの相対にする (警告線を出した時点の位置で固定)")] bool isRelative;
        [SerializeField] GameObject bodyPrefab;
        [SerializeField, Min(0f), Tooltip("秒")] float bodyDuration = 1f;
        [SerializeField] bool showWarning = true;
        [SerializeField] GameObject warningPrefab;
        [SerializeField, Min(0)] int warningFrames = 60;

        public override string Summary => $"画像ビーム / 警告 {warningFrames}f / {bodyDuration:0.##}s";

        public override INode Build(BuildScope scope) => new ActionNode(ctx =>
        {
            Fire(ctx);
            return NodeStatus.Success;
        });

        void Fire(BattleContext ctx)
        {
            var segment = line;
            if (isRelative && ctx.Self != null)
            {
                var origin = (Vector2)ctx.Self.Body.Position;
                segment.start += origin;
                segment.end += origin;
            }

            void SpawnAt(GameObject prefab, float duration) => ctx.Services.Spawner?.Spawn(new ActorSpawn
            {
                Prefab = prefab,
                Position = segment.start,
                Rotation = segment.Rotation,
                BeamRange = segment.Length,
                BeamDuration = duration,
            });

            if (showWarning && warningPrefab != null)
            {
                SpawnAt(warningPrefab, warningFrames * BattleWorld.DeltaTime);
                ctx.World.Schedule(warningFrames, () => SpawnAt(bodyPrefab, bodyDuration), ctx.Self);
            }
            else
            {
                SpawnAt(bodyPrefab, bodyDuration);
            }
        }
    }

    // 旧 MoveTrailFanBulletSignal。この敵が移動しながら軌跡上に弾を置き、全弾同時に扇状へ放つ
    [Serializable, NodeInfo("移動しながら軌跡上に弾を置き、移動後に全弾を扇状に放つ", "Action/Attack")]
    public sealed class MoveTrailFanNodeDefinition : ActionNodeDefinition
    {
        [SerializeField] Vector3 targetOffset = new(-3f, 0f, 0f);
        [SerializeField] bool isRelative = true;
        [SerializeField, Min(0f), Tooltip("秒")] float moveDuration = 1f;
        [SerializeField] MotionEase moveEase = MotionEase.Of(DG.Tweening.Ease.Linear);
        [SerializeField] bool emitBullets = true;
        [SerializeField] BulletSpec bullet = new();
        [SerializeReference, SubclassSelector] DirectionSource direction = new FixedDirection();
        [SerializeReference, SubclassSelector] RotationSource rotation = new InheritRotation();
        [SerializeField, Min(1)] int wayCount = 13;
        [SerializeField] float spreadAngle = 15f;
        [SerializeField, Min(0)] int releaseDelayFrames = 18;
        [SerializeField] SeType se;

        public override string Summary => emitBullets ? $"移動しつつ {wayCount}発 / {moveDuration:0.##}s" : $"移動 {moveDuration:0.##}s";

        public override INode Build(BuildScope scope) => new TrailNode(this);

        sealed class TrailNode : Node
        {
            readonly MoveTrailFanNodeDefinition def;
            MotionSequence move;
            Vector3 from;
            Vector3 to;
            int spawned;
            int elapsedFrames;
            int moveFrames;
            Vector2 baseDirection;
            Quaternion rot;

            public TrailNode(MoveTrailFanNodeDefinition def) => this.def = def;

            protected override void OnEnter(BattleContext ctx)
            {
                spawned = 0;
                elapsedFrames = 0;
                if (ctx.Self == null) return;

                var step = new MoveToMotion(def.targetOffset, def.isRelative, def.moveDuration, def.moveEase);
                from = ctx.Self.Body.Position;
                to = step.ResolveDestination(from);
                move = new MotionSequence(new MotionStep[] { step }, ctx.Self.Body, default);
                moveFrames = Mathf.Max(1, Mathf.RoundToInt(def.moveDuration * BattleWorld.FramesPerSecond));

                var e = new EmitContext(ctx, Vector2.zero);
                baseDirection = (def.direction ?? new FixedDirection()).GetDirection(e);
                rot = def.rotation?.GetRotation(e) ?? Quaternion.identity;
                if (def.emitBullets) Emitter.PlaySe(ctx, def.se);
            }

            protected override NodeStatus OnTick(BattleContext ctx)
            {
                if (move == null) return NodeStatus.Failure;

                move.Tick(BattleWorld.DeltaTime);
                elapsedFrames++;
                if (def.emitBullets) SpawnReached(ctx);
                return move.IsFinished && (!def.emitBullets || spawned >= def.wayCount) ? NodeStatus.Success : NodeStatus.Running;
            }

            // 移動の進捗が i/(n-1) に達した弾を、その比率の位置に置く。全弾の放出時刻は揃える
            void SpawnReached(BattleContext ctx)
            {
                var n = def.wayCount;
                var progress = move.IsFinished ? 1f : Mathf.Clamp01(elapsedFrames / (float)moveFrames);
                var baseAngle = Mathf.Atan2(baseDirection.y, baseDirection.x) * Mathf.Rad2Deg;
                var releaseFrame = moveFrames + def.releaseDelayFrames;

                while (spawned < n)
                {
                    var ratio = n == 1 ? 0f : spawned / (float)(n - 1);
                    if (ratio > progress + 1e-4f) break;

                    var angle = n == 1 ? baseAngle
                        : n % 2 == 1 ? baseAngle - def.spreadAngle * (n - 1) / 2f + def.spreadAngle * spawned
                        : baseAngle - def.spreadAngle / 2f - def.spreadAngle * (n / 2 - 1) + def.spreadAngle * spawned;
                    var dir = n == 1 ? baseDirection : new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
                    var pos = (Vector2)Vector3.Lerp(from, to, ratio);
                    Emitter.Spawn(ctx, def.bullet, Vector2.zero, _ => pos, dir, rot, 0, Mathf.Max(0, releaseFrame - elapsedFrames));
                    spawned++;
                }
            }

            protected override void OnExit(BattleContext ctx)
            {
                move?.Stop();
                move = null;
            }
        }
    }
}
