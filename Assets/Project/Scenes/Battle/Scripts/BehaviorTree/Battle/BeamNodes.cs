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

    public enum BeamOrigin
    {
        Self,
        World,
    }

    // 予測線 1 本分。Start から Direction へ Length の長さ (0 なら予測線 Prefab の既定の長さ)
    public struct BeamLine
    {
        public Vector2 Start;
        public Vector2 Direction;
        public float Length;
    }

    // ビームが撃つ位置と向きを決めた結果。WarningLine が先に作って予測線を出し、同じものを子のビームに渡す
    public abstract class BeamAim
    {
        public readonly List<BeamLine> Lines = new();
    }

    // WarningLine の子に置けるビームの定義
    public interface IBeamShape
    {
        BeamAim CreateAim(BattleContext ctx, BuildScope scope);
    }

    // 実行時のビーム。WarningLine から狙いを受け取ったら、自分で狙い直さずにそれで撃つ
    public interface IAimableBeam
    {
        void UseAim(BeamAim aim);
    }

    // 子のビームから線の形を聞いて予測線を出し、Frames 待ってから子を実行する
    [Serializable, NodeInfo("予測線を出して待ってから、子のビームを撃つ。予測線とビームの位置・向きは開始時に 1 回だけ決めて揃える", "Decorator")]
    public sealed class WarningLineNodeDefinition : DecoratorNodeDefinition
    {
        [SerializeField, Tooltip("予測線の Prefab。未指定なら予測線を出さずに待つだけ")] GameObject prefab;
        [SerializeField, Min(0), Tooltip("子のビームを撃つまでのフレーム数")] int frames = 60;
        [SerializeField, Min(0), Tooltip("予測線を表示するフレーム数。0 なら Frames と同じ")] int displayFrames;
        [SerializeField, Min(0f), Tooltip("予測線の太さ。0 なら Prefab の値")] float width;
        [SerializeField, Tooltip("ON: ビームを撃つまで Running (後ろのノードを待たせる)。OFF: 予測線を出したらすぐ完了し、ビームは予約で撃つ (途中で打ち切られても撃つ)")]
        bool waitForBeam = true;

        public WarningLineNodeDefinition() { }

        public WarningLineNodeDefinition(GameObject prefab, int frames, int displayFrames, float width, bool waitForBeam = true)
        {
            this.prefab = prefab;
            this.frames = frames;
            this.displayFrames = displayFrames;
            this.width = width;
            this.waitForBeam = waitForBeam;
        }

        public bool WaitForBeam
        {
            get => waitForBeam;
            set => waitForBeam = value;
        }

        public override string Summary => (prefab ? prefab.name : "(予測線なし)") + $" / {frames}f" + (waitForBeam ? "" : " / 待たない");

        public override INode Build(BuildScope scope)
        {
            var shape = Child as IBeamShape;
            if (Child != null && shape == null) Debug.LogWarning($"[BehaviorTree] WarningLine の子がビームではありません: {Child.GetType().Name}");
            return new WarningLineNode(this, shape, scope, BuildChild(scope));
        }

        sealed class WarningLineNode : Node
        {
            readonly WarningLineNodeDefinition def;
            readonly IBeamShape shape;
            readonly BuildScope scope;
            readonly INode child;
            BeamAim aim;
            int elapsed;
            bool childStarted;

            public WarningLineNode(WarningLineNodeDefinition def, IBeamShape shape, BuildScope scope, INode child)
            {
                this.def = def;
                this.shape = shape;
                this.scope = scope;
                this.child = child;
            }

            protected override void OnEnter(BattleContext ctx)
            {
                elapsed = 0;
                childStarted = false;
                aim = shape?.CreateAim(ctx, scope);
                if (aim == null || def.prefab == null) return;

                var display = (def.displayFrames > 0 ? def.displayFrames : def.frames) * BattleWorld.DeltaTime;
                var z = ctx.Self != null ? ctx.Self.Body.Position.z : 0f;
                foreach (var line in aim.Lines)
                {
                    ctx.Services.Spawner?.Spawn(new ActorSpawn
                    {
                        Prefab = def.prefab,
                        Position = new Vector3(line.Start.x, line.Start.y, z),
                        Rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(line.Direction.y, line.Direction.x) * Mathf.Rad2Deg),
                        BeamRange = line.Length,
                        BeamDuration = display,
                        BeamWidth = def.width,
                    });
                }
            }

            protected override NodeStatus OnTick(BattleContext ctx)
            {
                if (!def.waitForBeam)
                {
                    // ビームのノードは撃つ瞬間に予約まで済ませて即完了するので、1 回 Tick すれば足りる
                    var captured = aim;
                    ctx.World.Schedule(def.frames, () =>
                    {
                        if (captured != null && child is IAimableBeam beam) beam.UseAim(captured);
                        child.Tick(ctx);
                    }, ctx.Self);
                    return NodeStatus.Success;
                }

                if (elapsed < def.frames)
                {
                    elapsed++;
                    return NodeStatus.Running;
                }
                if (!childStarted)
                {
                    childStarted = true;
                    if (aim != null && child is IAimableBeam beam) beam.UseAim(aim);
                }
                return child.Tick(ctx);
            }

            protected override void OnAbort(BattleContext ctx)
            {
                if (childStarted) child.Abort(ctx);
            }
        }
    }

    // 同じ位置・向きへ弾を一定間隔で連射してビームにする。向きと本数は開始時に 1 回だけ決め、Pattern の各方向へ 1 本ずつ出す
    [Serializable, NodeInfo("同じ向きに弾を連射してビームにする。予測線は WarningLine で包んで出す", "Action/Attack")]
    public sealed class BulletBeamNodeDefinition : ActionNodeDefinition, IBeamShape
    {
        [SerializeField, Tooltip("Self: 撃つ敵の位置 + Position (連射ごとにその時点の位置) / World: Position をワールド座標として使う")]
        BeamOrigin origin = BeamOrigin.Self;
        [SerializeField] Vector2 position;
        [SerializeReference, SubclassSelector] DirectionSource direction = new FixedDirection();
        [SerializeReference, SubclassSelector, Tooltip("ビームの本数と角度")] ShotPattern pattern = new SingleShot();
        [SerializeReference, SubclassSelector] RotationSource rotation = new FixedRotation();
        [SerializeField, Tooltip("弾の飛距離と予測線の長さ。0 なら弾/予測線 Prefab の値")] float range;
        [SerializeField, Tooltip("1 回の連射で撃つ弾。複数並べると同じ位置・向きに重ねて撃つ")] List<BulletSpec> bullets = new() { new BulletSpec() };
        [SerializeField, Min(1)] int shotCount = 36;
        [SerializeField, Min(1)] int shotIntervalFrames = 5;
        [SerializeField] SeType se;

        public override string Summary =>
            $"{pattern?.Summary ?? "?"} / {direction?.Summary ?? "?"} / {string.Join(", ", bullets)} × {shotCount}発 {shotIntervalFrames}f おき";

        sealed class Aim : BeamAim
        {
            public Shot[] Shots;
            public Vector2 FixedOrigin;
            public Vector2 Muzzle;
            public Quaternion Rotation;
        }

        public BeamAim CreateAim(BattleContext ctx, BuildScope scope)
        {
            var muzzle = origin == BeamOrigin.Self ? position : Vector2.zero;
            var e = new EmitContext(ctx, muzzle);
            var shots = new List<Shot>();
            (pattern ?? new SingleShot()).Build(scope, (direction ?? new FixedDirection()).GetDirection(e), shots);
            var aim = new Aim
            {
                Shots = shots.ToArray(),
                FixedOrigin = origin == BeamOrigin.World ? position : e.Origin,
                Muzzle = muzzle,
                Rotation = rotation?.GetRotation(e) ?? Quaternion.identity,
            };
            foreach (var shot in aim.Shots)
            {
                aim.Lines.Add(new BeamLine { Start = aim.FixedOrigin + shot.Offset, Direction = shot.Direction, Length = range });
            }
            return aim;
        }

        public override INode Build(BuildScope scope) => new BeamNode(this, scope);

        sealed class BeamNode : Node, IAimableBeam
        {
            readonly BulletBeamNodeDefinition def;
            readonly BuildScope scope;
            BeamAim presetAim;

            public BeamNode(BulletBeamNodeDefinition def, BuildScope scope)
            {
                this.def = def;
                this.scope = scope;
            }

            public void UseAim(BeamAim aim) => presetAim = aim;

            protected override NodeStatus OnTick(BattleContext ctx)
            {
                var aim = presetAim as Aim ?? (Aim)def.CreateAim(ctx, scope);
                presetAim = null;
                def.Fire(ctx, aim);
                return NodeStatus.Success;
            }
        }

        void Fire(BattleContext ctx, Aim aim)
        {
            var rangeOverride = range > 0f ? range : -1f;
            for (var i = 0; i < shotCount; i++)
            {
                ctx.World.Schedule(i * shotIntervalFrames, () =>
                {
                    Emitter.PlaySe(ctx, se);
                    foreach (var shot in aim.Shots)
                    {
                        var offset = shot.Offset;
                        Func<EmitContext, Vector2> at = origin == BeamOrigin.World ? _ => aim.FixedOrigin + offset : x => x.Origin + offset;
                        foreach (var bullet in bullets)
                        {
                            Emitter.Spawn(ctx, bullet, aim.Muzzle, at, shot.Direction, aim.Rotation,
                                shot.SpawnDelayFrames, shot.StartDelayFrames, null, rangeOverride);
                        }
                    }
                }, ctx.Self);
            }
        }
    }

    // 1 枚絵のビーム本体を出す
    [Serializable, NodeInfo("画像のビーム本体を出す。予測線は WarningLine で包んで出す", "Action/Attack")]
    public sealed class SpriteBeamNodeDefinition : ActionNodeDefinition, IBeamShape
    {
        [SerializeField] BeamSegment line = new() { start = new Vector2(5f, 0f), end = new Vector2(-9f, 0f) };
        [SerializeField, Tooltip("ON: 線分を撃つ敵の位置からの相対にする (狙いを決めた時点の位置で固定)")] bool isRelative;
        [SerializeField] GameObject bodyPrefab;
        [SerializeField, Min(0f), Tooltip("秒")] float bodyDuration = 1f;

        public override string Summary => $"画像ビーム / {bodyDuration:0.##}s";

        sealed class Aim : BeamAim
        {
            public BeamSegment Segment;
        }

        public BeamAim CreateAim(BattleContext ctx, BuildScope scope)
        {
            var segment = line;
            if (isRelative && ctx.Self != null)
            {
                var origin = (Vector2)ctx.Self.Body.Position;
                segment.start += origin;
                segment.end += origin;
            }
            var aim = new Aim { Segment = segment };
            aim.Lines.Add(new BeamLine { Start = segment.start, Direction = segment.Direction, Length = segment.Length });
            return aim;
        }

        public override INode Build(BuildScope scope) => new BeamNode(this, scope);

        sealed class BeamNode : Node, IAimableBeam
        {
            readonly SpriteBeamNodeDefinition def;
            readonly BuildScope scope;
            BeamAim presetAim;

            public BeamNode(SpriteBeamNodeDefinition def, BuildScope scope)
            {
                this.def = def;
                this.scope = scope;
            }

            public void UseAim(BeamAim aim) => presetAim = aim;

            protected override NodeStatus OnTick(BattleContext ctx)
            {
                var aim = presetAim as Aim ?? (Aim)def.CreateAim(ctx, scope);
                presetAim = null;
                ctx.Services.Spawner?.Spawn(new ActorSpawn
                {
                    Prefab = def.bodyPrefab,
                    Position = aim.Segment.start,
                    Rotation = aim.Segment.Rotation,
                    BeamRange = aim.Segment.Length,
                    BeamDuration = def.bodyDuration,
                });
                return NodeStatus.Success;
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
