using System;
using System.Collections.Generic;
using Project.Scripts.Extensions;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.BehaviorTree
{
    public enum BeamOrigin
    {
        Self,
        World,
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
}
