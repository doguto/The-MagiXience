using System;
using System.Collections.Generic;
using Project.Scenes.Battle.Scripts.Presenter.Entity;
using Project.Scenes.Battle.Scripts.Runtime.Motion;
using Project.Scripts.Extensions;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.BehaviorTree
{
    // 弾を撃つノードの共通部分。撃ち方 (本数・角度・並べ方) は派生クラスが 1 つずつ持ち、グラフ上はノードの種類で区別できる
    [Serializable]
    public abstract class ShotNodeDefinition : ActionNodeDefinition
    {
        [SerializeField] BulletSpec bullet = new();
        [SerializeReference, SubclassSelector] DirectionSource direction = new FixedDirection();
        [SerializeReference, SubclassSelector] RotationSource rotation = new InheritRotation();
        [SerializeField, Tooltip("撃つ敵の位置からのずれ")] Vector2 muzzleOffset;
        [SerializeField] SeType se;

        protected abstract ShotPattern Pattern { get; }

        public override string Summary => $"{Pattern?.Summary ?? "?"} / {direction?.Summary ?? "?"} / {bullet}";

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
            Pattern?.Build(scope, baseDirection, shots);
            Emitter.PlaySe(ctx, se);

            foreach (var shot in shots)
            {
                var offset = shot.Offset;
                Func<EmitContext, Vector2> position = worldOrigin.HasValue
                    ? _ => worldOrigin.Value + offset
                    : at => at.Origin + offset;
                Emitter.Spawn(ctx, spec, muzzleOffset, position, shot.Direction, rot, shot.SpawnDelayFrames, shot.StartDelayFrames);
            }
        }

        // 旧 Fire ノードからの移し替え用
        internal void CopyCommonFrom(BulletSpec bullet, DirectionSource direction, RotationSource rotation, Vector2 muzzleOffset, SeType se)
        {
            this.bullet = bullet;
            this.direction = direction;
            this.rotation = rotation;
            this.muzzleOffset = muzzleOffset;
            this.se = se;
        }
    }

    [Serializable, NodeInfo("基準の向きに 1 発撃つ", "Action/Attack")]
    public sealed class SingleShotNodeDefinition : ShotNodeDefinition
    {
        [SerializeField] SingleShot pattern = new();
        protected override ShotPattern Pattern => pattern;
    }

    [Serializable, NodeInfo("扇状に同時に撃つ。奇数は中央に 1 発、偶数は中央を空ける", "Action/Attack")]
    public sealed class NWayShotNodeDefinition : ShotNodeDefinition
    {
        [SerializeField] NWayShot pattern = new();
        protected override ShotPattern Pattern => pattern;
    }

    [Serializable, NodeInfo("扇状に 1 発ずつ並べ、最後の 1 発のあと全弾同時に動き出す", "Action/Attack")]
    public sealed class SequentialFanShotNodeDefinition : ShotNodeDefinition
    {
        [SerializeField] SequentialFanShot pattern = new();
        protected override ShotPattern Pattern => pattern;
    }

    [Serializable, NodeInfo("円周上に 1 発ずつ並べ、最後の 1 発のあと全弾同時に動き出す", "Action/Attack")]
    public sealed class SequentialCircleShotNodeDefinition : ShotNodeDefinition
    {
        [SerializeField] SequentialCircleShot pattern = new();
        protected override ShotPattern Pattern => pattern;
    }

    // 撃つ攻撃ではないので ShotNodeDefinition に乗せない (向き・弾の動きの欄は使わない)
    [Serializable, NodeInfo("周囲に弾を並べ、渦を巻きながら中心へ集める溜めの演出", "Action/Effect")]
    public sealed class ChargeEffectNodeDefinition : ActionNodeDefinition
    {
        [SerializeField] BulletEntityPresenter prefab;
        [SerializeField, Tooltip("ツリーの Object パラメータ名。指定すると Prefab の代わりにその値を使う")] string prefabParam;
        [SerializeField] BtInt count = 16;
        [SerializeField] BtFloat radius = 1.5f;
        [SerializeField] BtFloat targetRadius = 0f;
        [SerializeField] BtInt gatherFrames = 60;
        [SerializeField, Tooltip("度/秒。負で時計回り")] BtFloat rotationSpeed = 480f;
        [SerializeField] SeType se;

        public override string Summary =>
            $"溜め {count}発 / {gatherFrames.ToFrameString()} / {(string.IsNullOrEmpty(prefabParam) ? (prefab ? prefab.name : "(弾未設定)") : "$" + prefabParam)}";

        public override INode Build(BuildScope scope)
        {
            var spec = new BulletSpec { prefab = prefab, prefabParam = prefabParam, damage = 0, affectedByWind = false }.Resolve(scope);
            var n = Mathf.Max(1, count.Resolve(scope));
            var r = Mathf.Max(0f, radius.Resolve(scope));
            var seconds = Mathf.Max(1, gatherFrames.Resolve(scope)) / (float)BattleContext.FramesPerSecond;
            var motion = new MotionStep[] { new SpiralMotion(r, Mathf.Max(0f, targetRadius.Resolve(scope)), seconds, rotationSpeed.Resolve(scope)) };
            return new ActionNode(ctx =>
            {
                Emitter.PlaySe(ctx, se);
                var rot = ctx.Self != null ? ctx.Self.Body.Rotation : Quaternion.identity;
                for (var i = 0; i < n; i++)
                {
                    var dir = ShotPattern.Rotate(360f / n * i);
                    var offset = dir * r;
                    Emitter.Spawn(ctx, spec, Vector2.zero, at => at.Origin + offset, dir, rot, 0, 0, motion);
                }
                return NodeStatus.Success;
            });
        }
    }

    [Serializable, NodeInfo("同じ扇を間隔をあけて何回か撃つ。発射位置の半径は 1 回目から最後へ線形に変わる", "Action/Attack")]
    public sealed class BurstShotNodeDefinition : ShotNodeDefinition
    {
        [SerializeField] BurstShot pattern = new();
        protected override ShotPattern Pattern => pattern;
    }
}
