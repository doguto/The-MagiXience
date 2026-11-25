using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Runtime.Motion
{
    // 旧 PullMovementHelper 相当の時間管理。
    // legacyOutQuad: 旧実装で Ease 未指定だった移動に DOTween 既定の OutQuad が掛かっていた挙動の再現。
    // 実時間 t に対して内部時間 τ = D(2u - u²), u = t/D。無限 (D=999999) では約 2 倍速になる。
    public struct StepClock
    {
        const float InfiniteDuration = 999999f;

        readonly float duration;
        readonly bool legacyOutQuad;
        float raw;

        public StepClock(float duration, bool legacyOutQuad)
        {
            this.duration = duration <= 0f ? InfiniteDuration : duration;
            this.legacyOutQuad = legacyOutQuad;
            raw = 0f;
            Elapsed = 0f;
        }

        // 移動計算に使う経過時間 (写像後)
        public float Elapsed { get; private set; }

        // dt 進め、移動計算に使う差分時間を返す
        public float Advance(float dt, out bool finished)
        {
            var before = Elapsed;
            raw = Mathf.Min(raw + dt, duration);
            finished = raw >= duration;
            Elapsed = legacyOutQuad ? duration * OutQuad(raw / duration) : raw;
            return Elapsed - before;
        }

        static float OutQuad(float u) => u * (2f - u);
    }

    // 毎フレームの差分で動かす移動の共通部分 (旧 PullMovementHelper)。初回 Tick で OnStart を呼ぶ。
    public abstract class PullRunner : MotionRunner
    {
        StepClock clock;
        bool started;

        protected void SetClock(float duration, bool legacyOutQuad) => clock = new StepClock(duration, legacyOutQuad);
        protected float Elapsed => clock.Elapsed;

        public sealed override bool Tick(float dt)
        {
            if (!started)
            {
                started = true;
                OnStart();
            }
            var step = clock.Advance(dt, out var finished);
            OnUpdate(step);
            return finished;
        }

        protected virtual void OnStart() { }
        protected abstract void OnUpdate(float dt);
    }
}
