using System;
using System.Collections.Generic;
using Project.Scenes.Battle.Scripts.Runtime.Motion;
using Project.Scripts.Extensions;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.BehaviorTree
{
    public struct Shot
    {
        public Vector2 Direction;
        public Vector2 Offset;
        public int SpawnDelayFrames;
        public int StartDelayFrames;
        public IReadOnlyList<MotionStep> Motion;
    }

    [Serializable]
    public abstract class ShotPattern
    {
        public abstract void Build(BuildScope scope, Vector2 baseDirection, List<Shot> shots);
        public virtual string Summary => GetType().Name.Replace("Shot", "");

        internal static Vector2 Rotate(float degrees)
        {
            var rad = degrees * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
        }

        internal static float Angle(Vector2 v) => Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg;

        // 旧 NWay の角度。奇数は中央に 1 発、偶数は中央を空ける
        internal static float FanAngle(float baseAngle, int count, float spread, int i) =>
            count % 2 == 1
                ? baseAngle - spread * (count - 1) / 2f + spread * i
                : baseAngle - spread / 2f - spread * (count / 2 - 1) + spread * i;
    }

    [Serializable]
    public sealed class SingleShot : ShotPattern
    {
        public override void Build(BuildScope scope, Vector2 baseDirection, List<Shot> shots) => shots.Add(new Shot { Direction = baseDirection });
        public override string Summary => "単発";
    }

    // 旧 NWaySignal。同時に扇状に撃つ
    [Serializable]
    public sealed class NWayShot : ShotPattern
    {
        [SerializeField, Min(1)] int wayCount = 3;
        [SerializeField, Tooltip("隣り合う弾の角度 (度)")] float spreadAngle = 60f;
        [SerializeField, Tooltip("発射位置を方向へずらす距離")] float radius;
        [SerializeField, Min(0), Tooltip("動き出すまでのフレーム数")] int startDelayFrames;
        [SerializeField, Tooltip("基準方向からの回転 (度, 反時計回り)")] float rotationOffset;

        public override void Build(BuildScope scope, Vector2 baseDirection, List<Shot> shots)
        {
            var baseAngle = Angle(baseDirection) + rotationOffset;
            for (var i = 0; i < wayCount; i++)
            {
                var dir = wayCount == 1 ? Rotate(baseAngle) : Rotate(FanAngle(baseAngle, wayCount, spreadAngle, i));
                shots.Add(new Shot
                {
                    Direction = dir,
                    Offset = radius != 0f ? dir * radius : Vector2.zero,
                    StartDelayFrames = startDelayFrames,
                });
            }
        }

        public override string Summary => $"{wayCount}way {spreadAngle:0.#}°";
    }

    // 旧 SequentialFanBulletSignal。1 発ずつ出し、全弾同時に動き出す
    [Serializable]
    public sealed class SequentialFanShot : ShotPattern
    {
        [SerializeField, Min(1)] int wayCount = 13;
        [SerializeField] float spreadAngle = 15f;
        [SerializeField, Min(0)] int spawnIntervalFrames = 6;
        [SerializeField, Min(0), Tooltip("最後の 1 発を出してから動き出すまで")] int releaseDelayFrames = 18;

        public override void Build(BuildScope scope, Vector2 baseDirection, List<Shot> shots)
        {
            var baseAngle = Angle(baseDirection);
            for (var i = 0; i < wayCount; i++)
            {
                shots.Add(new Shot
                {
                    Direction = wayCount == 1 ? baseDirection : Rotate(FanAngle(baseAngle, wayCount, spreadAngle, i)),
                    SpawnDelayFrames = i * spawnIntervalFrames,
                    StartDelayFrames = (wayCount - 1 - i) * spawnIntervalFrames + releaseDelayFrames,
                });
            }
        }

        public override string Summary => $"順に扇 {wayCount}発 / {spawnIntervalFrames}f 間隔";
    }

    // 旧 SequentialCircleBulletSignal。円周上に 1 発ずつ出し、全弾同時に動き出す
    [Serializable]
    public sealed class SequentialCircleShot : ShotPattern
    {
        [SerializeField, Min(1)] int wayCount = 12;
        [SerializeField] float radius = 1.5f;
        [SerializeField, Min(0)] int spawnIntervalFrames = 5;
        [SerializeField, Min(0)] int releaseDelayFrames = 12;

        public override void Build(BuildScope scope, Vector2 baseDirection, List<Shot> shots)
        {
            var baseAngle = Angle(baseDirection);
            for (var i = 0; i < wayCount; i++)
            {
                var dir = Rotate(baseAngle + 360f / wayCount * i);
                shots.Add(new Shot
                {
                    Direction = dir,
                    Offset = dir * radius,
                    SpawnDelayFrames = i * spawnIntervalFrames,
                    StartDelayFrames = (wayCount - 1 - i) * spawnIntervalFrames + releaseDelayFrames,
                });
            }
        }

        public override string Summary => $"順に円 {wayCount}発 / 半径 {radius:0.##}";
    }

    // 旧 ChargeSignal。周囲に弾を並べ、渦を巻きながら中心へ集める
    [Serializable]
    public sealed class ChargeShot : ShotPattern
    {
        [SerializeField] BtInt count = 16;
        [SerializeField] BtFloat radius = 1.5f;
        [SerializeField] BtFloat targetRadius = 0f;
        [SerializeField] BtInt gatherFrames = 60;
        [SerializeField, Tooltip("度/秒。負で時計回り")] BtFloat rotationSpeed = 480f;
        [SerializeField] BtInt startDelayFrames = 0;

        public ChargeShot() { }

        public ChargeShot(int count, float radius, float targetRadius, int gatherFrames, float rotationSpeed, int startDelayFrames)
        {
            this.count = count;
            this.radius = radius;
            this.targetRadius = targetRadius;
            this.gatherFrames = gatherFrames;
            this.rotationSpeed = rotationSpeed;
            this.startDelayFrames = startDelayFrames;
        }

        public override void Build(BuildScope scope, Vector2 baseDirection, List<Shot> shots)
        {
            var n = Mathf.Max(1, count.Resolve(scope));
            var r = Mathf.Max(0f, radius.Resolve(scope));
            var seconds = Mathf.Max(1, gatherFrames.Resolve(scope)) / (float)BattleContext.FramesPerSecond;
            var delay = Mathf.Max(0, startDelayFrames.Resolve(scope));
            var baseAngle = Angle(baseDirection);
            var motion = new MotionStep[] { new SpiralMotion(r, Mathf.Max(0f, targetRadius.Resolve(scope)), seconds, rotationSpeed.Resolve(scope)) };
            for (var i = 0; i < n; i++)
            {
                var dir = Rotate(baseAngle + 360f / n * i);
                shots.Add(new Shot { Direction = dir, Offset = dir * r, StartDelayFrames = delay, Motion = motion });
            }
        }

        public override string Summary => $"チャージ {count}発 / {gatherFrames.ToFrameString()}";
    }

    // 同じ扇を間隔をあけて何度か撃つ。発射位置の半径は1発目から最後へ線形に変わる
    [Serializable]
    public sealed class BurstShot : ShotPattern
    {
        [SerializeField, Min(1)] int wayCount = 5;
        [SerializeField, Tooltip("隣り合う弾の角度 (度)")] float spreadAngle = 72f;
        [SerializeField, Min(1)] int shotCount = 3;
        [SerializeField, Min(0)] int intervalFrames = 6;
        [SerializeField] float startRadius;
        [SerializeField] float endRadius;

        public override void Build(BuildScope scope, Vector2 baseDirection, List<Shot> shots)
        {
            var baseAngle = Angle(baseDirection);
            for (var k = 0; k < shotCount; k++)
            {
                var radius = shotCount == 1 ? startRadius : Mathf.Lerp(startRadius, endRadius, k / (float)(shotCount - 1));
                for (var i = 0; i < wayCount; i++)
                {
                    var dir = wayCount == 1 ? Rotate(baseAngle) : Rotate(FanAngle(baseAngle, wayCount, spreadAngle, i));
                    shots.Add(new Shot { Direction = dir, Offset = dir * radius, SpawnDelayFrames = k * intervalFrames });
                }
            }
        }

        public override string Summary => $"{wayCount}way {spreadAngle:0.#}° × {shotCount}連 / {intervalFrames}f";
    }

    [Serializable, NodeInfo("弾を撃つ。撃ち方 (Pattern) と方向・弾の設定を指定する", "Action/Attack")]
    public sealed class FireNodeDefinition : ActionNodeDefinition
    {
        [SerializeField] BulletSpec bullet = new();
        [SerializeReference, SubclassSelector] ShotPattern pattern = new SingleShot();
        [SerializeReference, SubclassSelector] DirectionSource direction = new FixedDirection();
        [SerializeReference, SubclassSelector] RotationSource rotation = new InheritRotation();
        [SerializeField, Tooltip("撃つ敵の位置からのずれ")] Vector2 muzzleOffset;
        [SerializeField] SeType se;

        public override string Summary => $"{pattern?.Summary ?? "?"} / {direction?.Summary ?? "?"} / {bullet}";

        public override INode Build(BuildScope scope)
        {
            var shots = new List<Shot>();
            var spec = bullet.Resolve(scope);
            return new ActionNode(ctx =>
            {
                Fire(ctx, scope, spec, shots);
                return NodeStatus.Success;
            });
        }

        void Fire(BattleContext ctx, BuildScope scope, BulletSpec spec, List<Shot> shots)
        {
            var e = new EmitContext(ctx, muzzleOffset);
            var dirSource = direction ?? new FixedDirection();
            var baseDirection = dirSource.GetDirection(e);
            var worldOrigin = dirSource.GetOrigin(e);
            var rot = rotation?.GetRotation(e) ?? Quaternion.identity;

            shots.Clear();
            (pattern ?? new SingleShot()).Build(scope, baseDirection, shots);
            Emitter.PlaySe(ctx, se);

            foreach (var shot in shots)
            {
                var offset = shot.Offset;
                Func<EmitContext, Vector2> position = worldOrigin.HasValue
                    ? _ => worldOrigin.Value + offset
                    : at => at.Origin + offset;
                Emitter.Spawn(ctx, spec, muzzleOffset, position, shot.Direction, rot, shot.SpawnDelayFrames, shot.StartDelayFrames, shot.Motion);
            }
        }
    }
}
