using System;
using Project.Scenes.Battle.Scripts.Presenter.Entity;
using Project.Scenes.Battle.Scripts.Runtime.Motion;
using Project.Scripts.Extensions;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.BehaviorTree
{
    // 撃つ攻撃ではないので ShotNode に乗せない (向き・弾の動き・ダメージの欄は使わない)
    [Serializable, NodeInfo("周囲に弾を並べ、渦を巻きながら中心へ集める溜めの演出", "Action/Effect")]
    public sealed class ChargeEffect : ActionNode
    {
        [SerializeField] BulletEntityPresenter prefab;
        [SerializeField, Tooltip("ツリーの Object パラメータ名。指定すると Prefab の代わりにその値を使う")] string prefabParam;
        [SerializeField] BtInt count = 16;
        [SerializeField] BtFloat radius = 1.5f;
        [SerializeField] BtFloat targetRadius = 0f;
        [SerializeField] BtInt gatherFrames = 60;
        [SerializeField, Tooltip("度/秒。負で時計回り")] BtFloat rotationSpeed = 480f;
        [SerializeField] SeType se;

        [NonSerialized] BulletSpec spec;
        [NonSerialized] int resolvedCount;
        [NonSerialized] float resolvedRadius;
        [NonSerialized] MotionStep[] motion;

        public override string Summary =>
            $"溜め {count}発 / {gatherFrames.ToFrameString()} / {(string.IsNullOrEmpty(prefabParam) ? (prefab ? prefab.name : "(弾未設定)") : "$" + prefabParam)}";

        protected override void OnSetup(TreeScope scope)
        {
            spec = new BulletSpec { prefab = prefab, prefabParam = prefabParam, damage = 0, affectedByWind = false }.Resolve(scope);
            resolvedCount = Mathf.Max(1, count.Resolve(scope));
            resolvedRadius = Mathf.Max(0f, radius.Resolve(scope));
            var seconds = Mathf.Max(1, gatherFrames.Resolve(scope)) / (float)BattleContext.FramesPerSecond;
            motion = new MotionStep[] { new SpiralMotion(resolvedRadius, Mathf.Max(0f, targetRadius.Resolve(scope)), seconds, rotationSpeed.Resolve(scope)) };
        }

        protected override NodeStatus OnTick(BattleContext ctx)
        {
            Emitter.PlaySe(ctx, se);
            var rot = ctx.Self != null ? ctx.Self.Body.Rotation : Quaternion.identity;
            for (var i = 0; i < resolvedCount; i++)
            {
                var dir = ShotPattern.Rotate(360f / resolvedCount * i);
                var offset = dir * resolvedRadius;
                Emitter.Spawn(ctx, spec, Vector2.zero, at => at.Origin + offset, dir, rot, 0, 0, motion);
            }
            return NodeStatus.Success;
        }
    }
}
