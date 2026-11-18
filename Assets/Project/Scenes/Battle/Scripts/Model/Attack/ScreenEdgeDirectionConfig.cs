using System;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Model.Attack
{
    /// <summary>画面端の発射位置を往復させ、反対側へ撃つ。風に合わせる場合は上流側から出す。</summary>
    [Serializable]
    public class ScreenEdgeDirectionConfig : IDirectionProvider, IAttackTimeDirectionProvider, IAttackOriginProvider
    {
        [SerializeField] bool fromLeft;
        [SerializeField, Tooltip("有効なら発射時の風向きへ撃つ。発射位置は風上の画面端になる。")]
        bool followWind;
        [SerializeField, Tooltip("画面端から内側への距離。負の値なら画面外から出す。")]
        float edgeInset = 0.02f;
        [SerializeField, Min(0f)] float verticalMargin = 0.6f;
        [SerializeField, Tooltip("進行方向に対して横方向の発射位置のずれ。並行弾の間隔を指定する。")]
        float lateralOffset;
        [SerializeField] float angularSpeed = 1.7f;
        [SerializeField] float attackTimeOffset = 0.9f;
        float time;

        public ScreenEdgeDirectionConfig() { }
        public ScreenEdgeDirectionConfig(bool fromLeft) => this.fromLeft = fromLeft;
        public ScreenEdgeDirectionConfig(float lateralOffset, float edgeInset, float margin)
        {
            followWind = true;
            this.lateralOffset = lateralOffset;
            this.edgeInset = edgeInset;
            verticalMargin = margin;
        }

        public void Initialize(Func<Vector3> player, Func<Vector3> source, Func<Quaternion> rotation) { }
        public void SetAttackTime(float attackTime) => time = Mathf.Max(0f, attackTime - attackTimeOffset);
        public Vector2 GetDirection() => followWind && BattleWind.Direction != WindDirection.None
            ? BattleWind.Vector : fromLeft ? Vector2.right : Vector2.left;
        public Vector2 GetOrigin()
        {
            var direction = GetDirection();
            float wave = (Mathf.Sin(time * angularSpeed) + 1f) * 0.5f;
            if (Mathf.Abs(direction.y) > Mathf.Abs(direction.x))
                return new Vector2(
                    Mathf.Lerp(ScreenBoundsCache.MinX + verticalMargin, ScreenBoundsCache.MaxX - verticalMargin, wave) + lateralOffset,
                    direction.y > 0f ? ScreenBoundsCache.MinY + edgeInset : ScreenBoundsCache.MaxY - edgeInset);
            return new Vector2(
                direction.x > 0f ? ScreenBoundsCache.MinX + edgeInset : ScreenBoundsCache.MaxX - edgeInset,
                Mathf.Lerp(ScreenBoundsCache.MinY + verticalMargin, ScreenBoundsCache.MaxY - verticalMargin, wave) + lateralOffset);
        }
        public IDirectionProvider Clone() => new ScreenEdgeDirectionConfig
        {
            fromLeft = fromLeft, edgeInset = edgeInset, verticalMargin = verticalMargin,
            angularSpeed = angularSpeed, attackTimeOffset = attackTimeOffset,
            followWind = followWind, lateralOffset = lateralOffset
        };
    }
}
