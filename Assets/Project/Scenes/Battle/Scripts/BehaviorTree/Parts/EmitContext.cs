using Project.Scenes.Battle.Scripts.Runtime;
using Project.Scenes.Battle.Scripts.Runtime.Motion;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.BehaviorTree
{
    // 発射時点の情報。撃つ敵がいなければ (ステージ直下の発射) Self は null
    public readonly struct EmitContext
    {
        public EmitContext(BattleContext ctx, Vector2 muzzleOffset)
        {
            Context = ctx;
            Self = ctx.Self;
            Origin = (Self != null ? (Vector2)Self.Body.Position : Vector2.zero) + muzzleOffset;
            SelfRotation = Self != null ? Self.Body.Rotation : Quaternion.identity;
            AttackTime = Self != null ? Self.AttackTime : ctx.Frame * BattleWorld.DeltaTime;
        }

        public BattleContext Context { get; }
        public BattleActor Self { get; }
        public Vector2 Origin { get; }
        public Quaternion SelfRotation { get; }
        public float AttackTime { get; }

        public Vector2 PlayerOrDefault(Vector2 fallback) =>
            BattleTargets.TryGetPlayerPosition(out var p) ? (Vector2)p : fallback;
    }
}
