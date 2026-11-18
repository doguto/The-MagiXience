using System;
using Project.Scenes.Battle.Scripts.Runtime;
using Project.Scenes.Battle.Scripts.Runtime.Motion;
using Project.Scripts.Extensions;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.BehaviorTree
{
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

            var e = new EmitContext(ctx, Vector2.zero, Scope);
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
