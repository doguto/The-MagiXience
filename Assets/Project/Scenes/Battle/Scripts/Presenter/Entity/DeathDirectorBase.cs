using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using Project.Scripts.Extensions;
using Project.Scripts.Presenter;

namespace Project.Scenes.Battle.Scripts.Presenter.Entity
{
    public abstract class DeathDirectorBase : MonoPresenter, IDeathDirector
    {
        [Header("Hit Stop")]
        [SerializeField] protected float hitStopDuration = 0.25f;

        [Header("Camera Shake")]
        [SerializeField] protected float shakeDuration = 0.5f;
        [SerializeField] protected float shakeStrength = 0.6f;
        [SerializeField] protected int shakeVibrato = 20;
        [SerializeField, Range(0f, 180f)] protected float shakeRandomness = 45f;

        [Header("Fade")]
        [SerializeField] protected float fadeDuration = 0.6f;
        [SerializeField] protected SpriteRenderer targetSpriteRenderer;

        [Header("Sound")]
        [SerializeField] protected SeType deathSeType = SeType.None;

        Tween cameraShakeTween;
        bool isPlaying;

        public async UniTask PlayAsync(CancellationToken ct)
        {
            if (isPlaying) return;
            isPlaying = true;

            // Pause中に死亡判定が走った場合、解除されるまで待ってから演出開始
            await UniTask.WaitWhile(() => Mathf.Approximately(Time.timeScale, 0f), cancellationToken: ct);

            try
            {
                if (deathSeType != SeType.None)
                {
                    soundManager?.PlaySE(deathSeType);
                }

                OnBeforeHitStop();

                var cam = Camera.main;
                if (cam != null && shakeDuration > 0f)
                {
                    cameraShakeTween = cam.transform.DOShakePosition(
                        shakeDuration,
                        shakeStrength,
                        shakeVibrato,
                        shakeRandomness,
                        snapping: false,
                        fadeOut: true,
                        randomnessMode: ShakeRandomnessMode.Harmonic);
                }

                await UniTask.Delay(
                    TimeSpan.FromSeconds(hitStopDuration),
                    DelayType.DeltaTime,
                    cancellationToken: ct);

                await PlayMainSequenceAsync(ct);
            }
            finally
            {
                cameraShakeTween?.Kill();
                cameraShakeTween = null;
                isPlaying = false;
            }
        }

        protected virtual void OnBeforeHitStop() { }

        protected abstract UniTask PlayMainSequenceAsync(CancellationToken ct);

        protected virtual void OnDestroy()
        {
            if (isPlaying)
            {
                Time.timeScale = 1f;
                cameraShakeTween?.Kill();
            }
        }
    }
}
