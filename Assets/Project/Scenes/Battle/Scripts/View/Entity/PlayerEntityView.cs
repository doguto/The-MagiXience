using UnityEngine;
using System.Collections;

namespace Project.Scenes.Battle.Scripts.View.Entity
{
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(Collider2D))]
    public class PlayerEntityView : MonoBehaviour
    {
        [SerializeField] SpriteRenderer spriteRenderer;
        [SerializeField] Animator animator;
        [SerializeField] ParticleSystem damageEffect;
        [SerializeField] ParticleSystem deathEffect;
        [SerializeField] ParticleSystem chargeEffect;
        [SerializeField] GameObject chargeIndicator;
        [SerializeField] float blinkInterval = 0.1f;

        Color originalColor;
        Coroutine blinkCoroutine;

        void Awake()
        {
            spriteRenderer ??= GetComponent<SpriteRenderer>();
            originalColor = spriteRenderer != null ? spriteRenderer.color : Color.white;

            if (chargeIndicator != null)
            {
                chargeIndicator.SetActive(false);
            }
        }

        public void UpdatePosition(Vector3 position)
        {
            transform.position = position;
        }

        public void PlayDamageEffect()
        {
            if (damageEffect != null)
            {
                damageEffect.Play();
            }
            
            if (spriteRenderer != null)
            {
                StartCoroutine(DamageFlashCoroutine());
            }
        }

        public void PlayDeathEffect()
        {
            if (deathEffect != null)
            {
                deathEffect.Play();
            }

            if (spriteRenderer != null)
            {
                spriteRenderer.enabled = false;
            }
        }

        public void UpdateHpDisplay(int currentHp, int maxHp)
        {
            if (animator != null)
            {
                float hpRatio = (float)currentHp / maxHp;
                animator.SetFloat("HpRatio", hpRatio);
            }
        }

        public void SetVisible(bool visible)
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.enabled = visible;
            }
        }

        public void SetSneakVisual(bool isSneaking)
        {
            if (spriteRenderer != null)
            {
                Color color = spriteRenderer.color;
                color.a = isSneaking ? 0.6f : 1f;
                spriteRenderer.color = color;
            }
        }

        public void UpdateChargeVisual(float chargeRatio)
        {
            if (chargeEffect != null)
            {
                if (chargeRatio > 0f && !chargeEffect.isPlaying)
                {
                    chargeEffect.Play();
                }
                else if (chargeRatio <= 0f && chargeEffect.isPlaying)
                {
                    chargeEffect.Stop();
                }
            }

            if (chargeIndicator != null)
            {
                chargeIndicator.SetActive(chargeRatio >= 1f);
            }
        }

        public void PlayShootEffect()
        {
            if (animator != null)
            {
                animator.SetTrigger("Shoot");
            }
        }

        public void SetInvincibilityVisual(bool isInvincible)
        {
            if (isInvincible)
            {
                if (blinkCoroutine == null)
                {
                    blinkCoroutine = StartCoroutine(BlinkCoroutine());
                }
            }
            else
            {
                if (blinkCoroutine != null)
                {
                    StopCoroutine(blinkCoroutine);
                    blinkCoroutine = null;
                }

                if (spriteRenderer != null)
                {
                    spriteRenderer.enabled = true;
                    spriteRenderer.color = originalColor;
                }
            }
        }

        IEnumerator DamageFlashCoroutine()
        {
            Color flashColor = originalColor;
            flashColor = Color.red;
            spriteRenderer.color = flashColor;
            yield return new WaitForSeconds(0.1f);
            spriteRenderer.color = originalColor;
        }

        IEnumerator BlinkCoroutine()
        {
            while (true)
            {
                if (spriteRenderer != null)
                {
                    spriteRenderer.enabled = true;
                }
                yield return new WaitForSeconds(blinkInterval);

                if (spriteRenderer != null)
                {
                    spriteRenderer.enabled = false;
                }
                yield return new WaitForSeconds(blinkInterval);
            }
        }
    }
}
