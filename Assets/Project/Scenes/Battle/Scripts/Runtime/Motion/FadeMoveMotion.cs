using System;
using DG.Tweening;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Project.Scenes.Battle.Scripts.Runtime.Motion
{
    // 旧 InvincibleFadeMoveMovementConfig。等速で動きながらフェードし、その間は無敵 (弾のみ)
    [Serializable]
    public sealed class FadeMoveMotion : MotionStep
    {
        [SerializeField, Min(0.01f)] float duration = 0.5f;
        [SerializeField] Vector3 velocity = Vector3.left;
        [SerializeField, Range(0f, 1f)] float startAlpha;
        [SerializeField, Range(0f, 1f)] float endAlpha = 1f;
        [SerializeField] bool invincible = true;

        public override bool GrantsInvincibility => invincible;
        public override string Summary => $"フェード {startAlpha:0.##}→{endAlpha:0.##} / {duration:0.##}s";

        public override MotionRunner CreateRunner()
        {
            var v = velocity;
            SpriteRenderer sprite = null;
            return new DelegatePullRunner(duration, true, (r, dt) =>
            {
                r.Target.Position += v * dt;
                if (sprite == null) return;
                var c = sprite.color;
                c.a = Mathf.Lerp(startAlpha, endAlpha, MotionEasing.Evaluate(MotionEasing.DefaultEase, r.Time / duration));
                sprite.color = c;
            }, r =>
            {
                if (r.LaunchDirection != Vector2.zero) v = (Vector3)r.LaunchDirection.normalized * velocity.magnitude;
                sprite = r.Target.Transform ? r.Target.Transform.GetComponent<SpriteRenderer>() : null;
                if (sprite == null) return;
                var c = sprite.color;
                c.a = startAlpha;
                sprite.color = c;
            }, invincible);
        }
    }
}
