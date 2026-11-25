using System;
using Project.Scenes.Battle.Scripts.Model;
using Project.Scenes.Battle.Scripts.Runtime;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.BehaviorTree
{
    // 旧 ScreenEdgeDirectionConfig。風上の画面端から撃つ。発射位置は発射時刻で端に沿って往復する
    [Serializable]
    public sealed class ScreenEdgeDirection : DirectionSource
    {
        [SerializeField] bool fromLeft;
        [SerializeField, Tooltip("風向きに合わせる")] bool followWind;
        [SerializeField, Tooltip("画面端からの内側距離。負で画面外")] float edgeInset = 0.02f;
        [SerializeField, Min(0f)] float verticalMargin = 0.6f;
        [SerializeField] float lateralOffset;
        [SerializeField] float angularSpeed = 1.7f;
        [SerializeField] float attackTimeOffset = 0.9f;

        public override Vector2 GetDirection(in EmitContext e)
        {
            if (followWind)
            {
                var wind = e.Context.Stage?.Get<WindService>();
                if (wind == null) e.Context.Stage?.WarnMissing<WindService>(nameof(ScreenEdgeDirection));
                else if (wind.Direction != WindDirection.None) return wind.Vector;
            }
            return fromLeft ? Vector2.right : Vector2.left;
        }

        public override Vector2? GetOrigin(in EmitContext e)
        {
            var dir = GetDirection(e);
            var t = Mathf.Max(0f, e.AttackTime - attackTimeOffset);
            var wave = (Mathf.Sin(t * angularSpeed) + 1f) * 0.5f;

            if (Mathf.Abs(dir.x) >= Mathf.Abs(dir.y))
            {
                var x = dir.x > 0f ? ScreenBoundsCache.MinX + edgeInset : ScreenBoundsCache.MaxX - edgeInset;
                var y = Mathf.Lerp(ScreenBoundsCache.MinY + verticalMargin, ScreenBoundsCache.MaxY - verticalMargin, wave) + lateralOffset;
                return new Vector2(x, y);
            }
            else
            {
                var y = dir.y > 0f ? ScreenBoundsCache.MinY + edgeInset : ScreenBoundsCache.MaxY - edgeInset;
                var x = Mathf.Lerp(ScreenBoundsCache.MinX + verticalMargin, ScreenBoundsCache.MaxX - verticalMargin, wave) + lateralOffset;
                return new Vector2(x, y);
            }
        }

        public override string Summary => followWind ? "画面端 (風上)" : fromLeft ? "画面左端" : "画面右端";
    }
}
