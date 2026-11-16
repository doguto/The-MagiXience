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

    // WarningLine の子に置けるビーム。狙いを受け取ったら、自分で狙い直さずにそれで撃つ
    public interface IBeam
    {
        BeamAim CreateAim(BattleContext ctx);
        void UseAim(BeamAim aim);
    }

    [Serializable, NodeInfo("予測線を出して待ってから、子のビームを撃つ。予測線とビームの位置・向きは開始時に 1 回だけ決めて揃える", "Decorator")]
    public sealed class WarningLine : DecoratorNode
    {
        [SerializeField, Tooltip("予測線の Prefab。未指定なら予測線を出さずに待つだけ")] GameObject prefab;
        [SerializeField, Min(0), Tooltip("子のビームを撃つまでのフレーム数")] int frames = 60;
        [SerializeField, Min(0), Tooltip("予測線を表示するフレーム数。0 なら Frames と同じ")] int displayFrames;
        [SerializeField, Min(0f), Tooltip("予測線の太さ。0 なら Prefab の値")] float width;
        [SerializeField, Tooltip("ON: ビームを撃つまで Running (後ろのノードを待たせる)。OFF: 予測線を出したらすぐ完了し、ビームは予約で撃つ (途中で打ち切られても撃つ)")]
        bool waitForBeam = true;

        [NonSerialized] IBeam beam;
        [NonSerialized] BeamAim aim;
        [NonSerialized] int elapsed;
        [NonSerialized] bool childStarted;

        public bool WaitForBeam
        {
            get => waitForBeam;
            set => waitForBeam = value;
        }

        public override string Summary => (prefab ? prefab.name : "(予測線なし)") + $" / {frames}f" + (waitForBeam ? "" : " / 待たない");

        protected override void OnSetup(TreeScope scope)
        {
            beam = Child as IBeam;
            if (beam == null && Child is not EmptyNode) Debug.LogWarning($"[BehaviorTree] WarningLine の子がビームではありません: {Child.GetType().Name}");
        }

        protected override void OnEnter(BattleContext ctx)
        {
            elapsed = 0;
            childStarted = false;
            aim = beam?.CreateAim(ctx);
            if (aim == null || prefab == null) return;

            var display = (displayFrames > 0 ? displayFrames : frames) * BattleWorld.DeltaTime;
            var z = ctx.Self != null ? ctx.Self.Body.Position.z : 0f;
            foreach (var line in aim.Lines)
            {
                ctx.Services.Spawner?.Spawn(new ActorSpawn
                {
                    Prefab = prefab,
                    Position = new Vector3(line.Start.x, line.Start.y, z),
                    Rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(line.Direction.y, line.Direction.x) * Mathf.Rad2Deg),
                    BeamRange = line.Length,
                    BeamDuration = display,
                    BeamWidth = width,
                });
            }
        }

        protected override NodeStatus OnTick(BattleContext ctx)
        {
            if (!waitForBeam)
            {
                // ビームのノードは撃つ瞬間に予約まで済ませて即完了するので、1 回 Tick すれば足りる
                var captured = aim;
                var child = Child;
                ctx.World.Schedule(frames, () =>
                {
                    if (captured != null) beam?.UseAim(captured);
                    child.Tick(ctx);
                }, ctx.Self);
                return NodeStatus.Success;
            }

            if (elapsed < frames)
            {
                elapsed++;
                return NodeStatus.Running;
            }
            if (!childStarted)
            {
                childStarted = true;
                if (aim != null) beam?.UseAim(aim);
            }
            return Child.Tick(ctx);
        }

        protected override void OnAbort(BattleContext ctx)
        {
            if (childStarted) Child.Abort(ctx);
        }
    }

    // 同じ位置・向きへ弾を一定間隔で連射してビームにする。向きと本数は開始時に 1 回だけ決め、Pattern の各方向へ 1 本ずつ出す
    [Serializable, NodeInfo("同じ向きに弾を連射してビームにする。予測線は WarningLine で包んで出す", "Action/Attack")]
    public sealed class BulletBeam : ActionNode, IBeam
    {
        [SerializeField, Tooltip("Self: 撃つ敵の位置 + Position (連射ごとにその時点の位置) / World: Position をワールド座標として使う")]
        BeamOrigin origin = BeamOrigin.Self;
        [SerializeField] Vector2 position;
        [SerializeReference, SubclassSelector] DirectionSource direction = new FixedDirection();
        [SerializeReference, SubclassSelector, Tooltip("ビームの本数と角度")] ShotPattern pattern = new SinglePattern();
        [SerializeReference, SubclassSelector] RotationSource rotation = new FixedRotation();
        [SerializeField, Tooltip("弾の飛距離と予測線の長さ。0 なら弾/予測線 Prefab の値")] float range;
        [SerializeField, Tooltip("1 回の連射で撃つ弾。複数並べると同じ位置・向きに重ねて撃つ")] List<BulletSpec> bullets = new() { new BulletSpec() };
        [SerializeField, Min(1)] int shotCount = 36;
        [SerializeField, Min(1)] int shotIntervalFrames = 5;
        [SerializeField] SeType se;

        [NonSerialized] BulletSpec[] resolvedBullets;
        [NonSerialized] BeamAim presetAim;

        public override string Summary =>
            $"{pattern?.Summary ?? "?"} / {direction?.Summary ?? "?"} / {string.Join(", ", bullets)} × {shotCount}発 {shotIntervalFrames}f おき";

        sealed class Aim : BeamAim
        {
            public Shot[] Shots;
            public Vector2 FixedOrigin;
            public Vector2 Muzzle;
            public Quaternion Rotation;
        }

        protected override void OnSetup(TreeScope scope)
        {
            resolvedBullets = new BulletSpec[bullets.Count];
            for (var i = 0; i < bullets.Count; i++) resolvedBullets[i] = bullets[i].Resolve(scope);
        }

        public BeamAim CreateAim(BattleContext ctx)
        {
            var muzzle = origin == BeamOrigin.Self ? position : Vector2.zero;
            var e = new EmitContext(ctx, muzzle);
            var shots = new List<Shot>();
            (pattern ?? new SinglePattern()).Build((direction ?? new FixedDirection()).GetDirection(e), shots);
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

        public void UseAim(BeamAim aim) => presetAim = aim;

        protected override NodeStatus OnTick(BattleContext ctx)
        {
            var aim = presetAim as Aim ?? (Aim)CreateAim(ctx);
            presetAim = null;
            Fire(ctx, aim);
            return NodeStatus.Success;
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
                        foreach (var bullet in resolvedBullets)
                        {
                            Emitter.Spawn(ctx, bullet, aim.Muzzle, at, shot.Direction, aim.Rotation,
                                shot.SpawnDelayFrames, shot.StartDelayFrames, null, rangeOverride);
                        }
                    }
                }, ctx.Self);
            }
        }
    }

    [Serializable, NodeInfo("画像のビーム本体を出す。予測線は WarningLine で包んで出す", "Action/Attack")]
    public sealed class SpriteBeam : ActionNode, IBeam
    {
        [SerializeField] BeamSegment line = new() { start = new Vector2(5f, 0f), end = new Vector2(-9f, 0f) };
        [SerializeField, Tooltip("ON: 線分を撃つ敵の位置からの相対にする (狙いを決めた時点の位置で固定)")] bool isRelative;
        [SerializeField] GameObject bodyPrefab;
        [SerializeField, Min(0f), Tooltip("秒")] float bodyDuration = 1f;

        [NonSerialized] BeamAim presetAim;

        public override string Summary => $"画像ビーム / {bodyDuration:0.##}s";

        sealed class Aim : BeamAim
        {
            public BeamSegment Segment;
        }

        public BeamAim CreateAim(BattleContext ctx)
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

        public void UseAim(BeamAim aim) => presetAim = aim;

        protected override NodeStatus OnTick(BattleContext ctx)
        {
            var aim = presetAim as Aim ?? (Aim)CreateAim(ctx);
            presetAim = null;
            ctx.Services.Spawner?.Spawn(new ActorSpawn
            {
                Prefab = bodyPrefab,
                Position = aim.Segment.start,
                Rotation = aim.Segment.Rotation,
                BeamRange = aim.Segment.Length,
                BeamDuration = bodyDuration,
            });
            return NodeStatus.Success;
        }
    }

    // 旧 MoveTrailFanBulletSignal。この敵が移動しながら軌跡上に弾を置き、全弾同時に扇状へ放つ
    [Serializable, NodeInfo("移動しながら軌跡上に弾を置き、移動後に全弾を扇状に放つ", "Action/Attack")]
    public sealed class MoveTrailFan : ActionNode
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

        [NonSerialized] BulletSpec resolvedBullet;
        [NonSerialized] MotionSequence move;
        [NonSerialized] Vector3 from;
        [NonSerialized] Vector3 to;
        [NonSerialized] int spawned;
        [NonSerialized] int elapsedFrames;
        [NonSerialized] int moveFrames;
        [NonSerialized] Vector2 baseDirection;
        [NonSerialized] Quaternion rot;

        public override string Summary => emitBullets ? $"移動しつつ {wayCount}発 / {moveDuration:0.##}s" : $"移動 {moveDuration:0.##}s";

        protected override void OnSetup(TreeScope scope) => resolvedBullet = bullet.Resolve(scope);

        protected override void OnEnter(BattleContext ctx)
        {
            spawned = 0;
            elapsedFrames = 0;
            if (ctx.Self == null) return;

            var step = new MoveToMotion(targetOffset, isRelative, moveDuration, moveEase);
            from = ctx.Self.Body.Position;
            to = step.ResolveDestination(from);
            move = new MotionSequence(new MotionStep[] { step }, ctx.Self.Body, default);
            moveFrames = Mathf.Max(1, Mathf.RoundToInt(moveDuration * BattleWorld.FramesPerSecond));

            var e = new EmitContext(ctx, Vector2.zero);
            baseDirection = (direction ?? new FixedDirection()).GetDirection(e);
            rot = rotation?.GetRotation(e) ?? Quaternion.identity;
            if (emitBullets) Emitter.PlaySe(ctx, se);
        }

        protected override NodeStatus OnTick(BattleContext ctx)
        {
            if (move == null) return NodeStatus.Failure;

            move.Tick(BattleWorld.DeltaTime);
            elapsedFrames++;
            if (emitBullets) SpawnReached(ctx);
            return move.IsFinished && (!emitBullets || spawned >= wayCount) ? NodeStatus.Success : NodeStatus.Running;
        }

        // 移動の進捗が i/(n-1) に達した弾を、その比率の位置に置く。全弾の放出時刻は揃える
        void SpawnReached(BattleContext ctx)
        {
            var n = wayCount;
            var progress = move.IsFinished ? 1f : Mathf.Clamp01(elapsedFrames / (float)moveFrames);
            var baseAngle = ShotPattern.Angle(baseDirection);
            var releaseFrame = moveFrames + releaseDelayFrames;

            while (spawned < n)
            {
                var ratio = n == 1 ? 0f : spawned / (float)(n - 1);
                if (ratio > progress + 1e-4f) break;

                var dir = n == 1 ? baseDirection : ShotPattern.Rotate(ShotPattern.FanAngle(baseAngle, n, spreadAngle, spawned));
                var pos = (Vector2)Vector3.Lerp(from, to, ratio);
                Emitter.Spawn(ctx, resolvedBullet, Vector2.zero, _ => pos, dir, rot, 0, Mathf.Max(0, releaseFrame - elapsedFrames));
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
