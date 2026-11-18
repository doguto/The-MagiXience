using System;
using DG.Tweening;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Model.Movement
{
    /// <summary>
    /// 指定時間、無敵状態のまま指定速度で移動しつつ、
    /// SpriteRendererのアルファをstartAlpha→endAlphaへTweenする移動ステップ。
    /// 出現直後にフェードインさせたい弾（DeathBallなど）向け。
    /// </summary>
    [Serializable]
    public class InvincibleFadeMoveMovementConfig : IMovementStep, IInvincibilityGrantingStep
    {
        [SerializeField, Min(0.01f)] float duration = 1f;
        [SerializeField, Tooltip("移動速度。direction指定時はこのmagnitudeのみ使用")]
        Vector3 velocity = Vector3.zero;
        [SerializeField, Range(0f, 1f)] float startAlpha = 0f;
        [SerializeField, Range(0f, 1f)] float endAlpha = 1f;

        public Tween Play(Transform target, Vector2 direction, Animator animator)
        {
            Vector3 vel = direction != Vector2.zero
                ? (Vector3)(direction.normalized) * velocity.magnitude
                : velocity;

            var moveTween = PullMovementHelper.Create(target, duration, (t, dt) => t.position += vel * dt);

            var spriteRenderer = target.GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
                return moveTween;

            var color = spriteRenderer.color;
            color.a = startAlpha;
            spriteRenderer.color = color;

            var fadeTween = spriteRenderer.DOFade(endAlpha, duration);

            return DOTween.Sequence().Join(moveTween).Join(fadeTween);
        }
    }
}
