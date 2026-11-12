using System;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Runtime.Motion
{
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

    sealed class DelegatePullRunner : PullRunner
    {
        readonly Action<DelegatePullRunner> onStart;
        readonly Action<DelegatePullRunner, float> onUpdate;
        readonly bool grantsInvincibility;

        public DelegatePullRunner(float duration, bool legacyOutQuad, Action<DelegatePullRunner, float> onUpdate,
            Action<DelegatePullRunner> onStart = null, bool grantsInvincibility = false)
        {
            SetClock(duration, legacyOutQuad);
            this.onUpdate = onUpdate;
            this.onStart = onStart;
            this.grantsInvincibility = grantsInvincibility;
        }

        public override bool GrantsInvincibility => grantsInvincibility;
        public IMotionBody Target => Body;
        public Vector2 LaunchDirection => Direction;
        public float LaunchAttackTime => AttackTime;
        public Vector2 LaunchWindAxis => WindAxis;
        public float Time => Elapsed;
        public void Reject() => Rejected = true;

        protected override void OnStart() => onStart?.Invoke(this);
        protected override void OnUpdate(float dt) => onUpdate(this, dt);
    }

    [Serializable]
    public sealed class WaitMotion : MotionStep
    {
        [SerializeField, Min(0f)] float duration = 1f;
        [SerializeField, Tooltip("待機中は無敵 (弾のみ有効)")] bool invincible;

        public override bool GrantsInvincibility => invincible;
        public override string Summary => $"{duration:0.##}s 待機" + (invincible ? " (無敵)" : "");

        public override MotionRunner CreateRunner() =>
            new DelegatePullRunner(Mathf.Max(0.0001f, duration), false, (_, _) => { }, grantsInvincibility: invincible);
    }

    // 旧 InfiniteMovementConfig。発射方向があればその向きに |velocity| の速さで進む
    [Serializable]
    public sealed class VelocityMotion : MotionStep
    {
        [SerializeField] Vector3 velocity = Vector3.left;
        [SerializeField, Min(0f), Tooltip("0 で無限")] float duration;

        public override string Summary => $"速度 {velocity.magnitude:0.##}" + (duration > 0f ? $" / {duration:0.##}s" : "");

        public override MotionRunner CreateRunner()
        {
            var v = velocity;
            return new DelegatePullRunner(duration, true, (r, dt) => r.Target.Position += v * dt, r =>
            {
                if (r.LaunchDirection != Vector2.zero) v = (Vector3)r.LaunchDirection.normalized * velocity.magnitude;
            });
        }
    }

    // 旧 TweenMovementConfig
    [Serializable]
    public sealed class MoveToMotion : MotionStep
    {
        [SerializeField] Vector3 targetOffset = new(-3f, 0f, 0f);
        [SerializeField, Tooltip("ON: 現在位置からの相対 / OFF: ワールド座標")] bool isRelative = true;
        [SerializeField, Min(0f)] float duration = 1f;
        [SerializeField] MotionEase ease = MotionEase.Of(DG.Tweening.Ease.Linear);

        public MoveToMotion() { }

        public MoveToMotion(Vector3 targetOffset, bool isRelative, float duration, MotionEase ease)
        {
            this.targetOffset = targetOffset;
            this.isRelative = isRelative;
            this.duration = duration;
            this.ease = ease;
        }

        public float Duration => duration;
        public Vector3 ResolveDestination(Vector3 from) => isRelative ? from + targetOffset : targetOffset;
        public override string Summary => (isRelative ? $"相対 {targetOffset}" : $"位置 {targetOffset}") + $" / {duration:0.##}s {ease}";

        public override MotionRunner CreateRunner() => new TweenRunner(duration, ease, b => ResolveDestination(b.Position));
    }

    // 旧 AimTweenMovementConfig
    [Serializable]
    public sealed class MoveToPlayerMotion : MotionStep
    {
        [SerializeField, Min(0f)] float duration = 1f;
        [SerializeField] bool x = true;
        [SerializeField] bool y = true;
        [SerializeField] MotionEase ease = MotionEase.Of(DG.Tweening.Ease.Linear);

        public override string Summary => $"自機へ ({(x ? "X" : "")}{(y ? "Y" : "")}) / {duration:0.##}s {ease}";

        public override MotionRunner CreateRunner()
        {
            if (!x && !y) return new DelegatePullRunner(Mathf.Max(0.0001f, duration), false, (_, _) => { });
            return new TweenRunner(duration, ease, b =>
            {
                var current = b.Position;
                var player = BattleTargets.TryGetPlayerPosition(out var p) ? p : current;
                return new Vector3(x ? player.x : current.x, y ? player.y : current.y, current.z);
            });
        }
    }

    sealed class TweenRunner : MotionRunner
    {
        readonly float duration;
        readonly MotionEase ease;
        readonly Func<IMotionBody, Vector3> resolveDestination;
        Vector3 from;
        Vector3 to;
        float elapsed;

        public TweenRunner(float duration, MotionEase ease, Func<IMotionBody, Vector3> resolveDestination)
        {
            this.duration = duration;
            this.ease = ease;
            this.resolveDestination = resolveDestination;
        }

        protected override void OnBegin()
        {
            from = Body.Position;
            to = resolveDestination(Body);
        }

        public override bool Tick(float dt)
        {
            elapsed += dt;
            if (duration <= 0f || elapsed >= duration)
            {
                Body.Position = to;
                return true;
            }
            Body.Position = Vector3.LerpUnclamped(from, to, ease.Evaluate(elapsed / duration));
            return false;
        }
    }

    // 旧 AimStraightMovementConfig
    [Serializable]
    public sealed class AimStraightMotion : MotionStep
    {
        [SerializeField] float speed = 5f;
        [SerializeField, Min(0f), Tooltip("0 で無限")] float duration;

        public override string Summary => $"自機狙い直進 {speed:0.##}";

        public override MotionRunner CreateRunner()
        {
            var v = Vector3.zero;
            return new DelegatePullRunner(duration, true, (r, dt) => r.Target.Position += v * dt, r =>
            {
                var pos = r.Target.Position;
                v = BattleTargets.TryGetPlayerPosition(out var player)
                    ? ((Vector3)((Vector2)(player - pos)).normalized) * speed
                    : Vector3.left * speed;
            });
        }
    }

    // 旧 ForwardMovementConfig。開始時の向き (right) に進む
    [Serializable]
    public sealed class ForwardMotion : MotionStep
    {
        [SerializeField] float speed = 5f;
        [SerializeField, Min(0f), Tooltip("0 で無限")] float duration;

        public override string Summary => $"前方へ {speed:0.##}";

        public override MotionRunner CreateRunner()
        {
            var v = Vector3.zero;
            return new DelegatePullRunner(duration, true, (r, dt) => r.Target.Position += v * dt,
                r => v = r.Target.Rotation * Vector3.right * speed);
        }
    }

    // 旧 HomingMovementConfig
    [Serializable]
    public sealed class HomingMotion : MotionStep
    {
        [SerializeField] float speed = 5f;
        [SerializeField, Tooltip("旋回速度 (度/秒)")] float turnSpeed = 90f;
        [SerializeField, Min(0f), Tooltip("0 で無限")] float duration;

        public override string Summary => $"ホーミング {speed:0.##} / 旋回 {turnSpeed:0.#}°";

        public override MotionRunner CreateRunner()
        {
            var dir = Vector3.left;
            return new DelegatePullRunner(duration, true, (r, dt) =>
            {
                if (BattleTargets.TryGetPlayerPosition(out var player))
                {
                    var toPlayer = (Vector3)((Vector2)(player - r.Target.Position)).normalized;
                    dir = Vector3.RotateTowards(dir, toPlayer, turnSpeed * Mathf.Deg2Rad * dt, 0f);
                }
                r.Target.Position += dir * speed * dt;
            }, r => dir = r.Target.Rotation * Vector3.right);
        }
    }

    // 旧 StraightWithHomingMovementConfig。常に左へ進みつつ Y を自機に寄せる
    [Serializable]
    public sealed class StraightWithHomingMotion : MotionStep
    {
        [SerializeField] float speed = 5f;
        [SerializeField] float verticalTrackingSpeed = 2f;
        [SerializeField, Min(0f), Tooltip("0 で無限")] float duration;

        public override string Summary => $"左進 {speed:0.##} / 縦追尾 {verticalTrackingSpeed:0.##}";

        public override MotionRunner CreateRunner() => new DelegatePullRunner(duration, true, (r, dt) =>
        {
            var p = r.Target.Position;
            p.x -= speed * dt;
            if (BattleTargets.TryGetPlayerPosition(out var player))
                p.y = Mathf.MoveTowards(p.y, player.y, verticalTrackingSpeed * dt);
            r.Target.Position = p;
        });
    }

    // 旧 SineMovementConfig。差分で加算するため外部の移動と合成できる
    [Serializable]
    public sealed class SineMotion : MotionStep
    {
        [SerializeField] Vector3 baseVelocity = Vector3.left;
        [SerializeField, Min(0f)] float amplitude = 1f;
        [SerializeField, Min(0.01f), Tooltip("Hz")] float frequency = 1f;
        [SerializeField, Min(0f), Tooltip("0 で無限")] float duration;

        public override string Summary => $"サイン波 振幅 {amplitude:0.##} / {frequency:0.##}Hz";

        public override MotionRunner CreateRunner()
        {
            var axis = new Vector3(-baseVelocity.y, baseVelocity.x, 0f).normalized;
            var prev = 0f;
            return new DelegatePullRunner(duration, true, (r, dt) =>
            {
                var s = Mathf.Sin(r.Time * frequency * 2f * Mathf.PI) * amplitude;
                r.Target.Position += baseVelocity * dt + axis * (s - prev);
                prev = s;
            });
        }
    }

    // 旧 AnchoredSineMovementConfig。絶対位置で動くため開始時に anchor へ吸着する
    [Serializable]
    public sealed class AnchoredSineMotion : MotionStep
    {
        [SerializeField] Vector3 anchor = new(5.7f, 0f, 0f);
        [SerializeField] Vector3 amplitude = new(0f, 1.4f, 0f);
        [SerializeField, Tooltip("rad/s")] float angularSpeed = 0.65f;
        [SerializeField, Min(0f)] float cueSeconds = 0.9f;
        [SerializeField] Vector3 cueOffset;
        [SerializeField, Min(0f), Tooltip("0 で無限")] float duration;

        public override string Summary => $"{anchor} 周りで揺れる";

        public override MotionRunner CreateRunner() => new DelegatePullRunner(duration, false, (r, _) =>
        {
            var e = r.Time;
            r.Target.Position = e < cueSeconds && cueSeconds > 0f
                ? anchor + cueOffset * Mathf.Sin(Mathf.PI * e / cueSeconds)
                : anchor + amplitude * Mathf.Sin((e - cueSeconds) * angularSpeed);
        });
    }

    // 旧 QuadraticMovementConfig
    [Serializable]
    public sealed class QuadraticMotion : MotionStep
    {
        [SerializeField] Vector3 initialVelocity = Vector3.left;
        [SerializeField] Vector3 acceleration;
        [SerializeField, Min(0f), Tooltip("0 で無限")] float duration;

        public override string Summary => $"初速 {initialVelocity} / 加速 {acceleration}";

        public override MotionRunner CreateRunner()
        {
            var v = initialVelocity;
            return new DelegatePullRunner(duration, false, (r, dt) =>
            {
                v += acceleration * dt;
                r.Target.Position += v * dt;
            });
        }
    }

    // 旧 AimRotateConfig。自機の方向へ回転する (位置は動かない)
    [Serializable]
    public sealed class AimRotateMotion : MotionStep
    {
        [SerializeField, Min(0f)] float duration = 0.5f;
        [SerializeField] DG.Tweening.Ease ease = DG.Tweening.Ease.InOutSine;

        public override string Summary => $"自機へ向く {duration:0.##}s";

        public override MotionRunner CreateRunner()
        {
            var start = Quaternion.identity;
            var end = Quaternion.identity;
            var d = Mathf.Max(0.001f, duration);
            return new DelegatePullRunner(d, false, (r, _) =>
            {
                r.Target.Rotation = Quaternion.SlerpUnclamped(start, end, MotionEasing.Evaluate(ease, r.Time / d));
            }, r =>
            {
                start = r.Target.Rotation;
                end = start;
                if (BattleTargets.TryGetPlayerPosition(out var player))
                {
                    var to = (Vector2)(player - r.Target.Position);
                    end = Quaternion.Euler(0f, 0f, Mathf.Atan2(to.y, to.x) * Mathf.Rad2Deg);
                }
            });
        }
    }

    // 旧 AnimationMovementConfig。Animator にクリップを再生させ、その長さだけ待つ
    [Serializable]
    public sealed class AnimationMotion : MotionStep
    {
        [SerializeField] AnimationClip clip;
        [SerializeField] int layerIndex;

        public override string Summary => clip ? $"アニメ {clip.name}" : "アニメ (未設定)";

        public override MotionRunner CreateRunner()
        {
            var length = clip ? clip.length : 0f;
            return new DelegatePullRunner(Mathf.Max(0.0001f, length), false, (_, _) => { }, r =>
            {
                var animator = r.Target.Transform ? r.Target.Transform.GetComponent<Animator>() : null;
                if (animator == null || clip == null)
                {
                    Debug.LogWarning("[AnimationMotion] Animator or clip is missing.");
                    return;
                }
                var overrideController = new AnimatorOverrideController(animator.runtimeAnimatorController);
                var overrides = new System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<AnimationClip, AnimationClip>>();
                overrideController.GetOverrides(overrides);
                if (overrides.Count > 0) overrides[0] = new(overrides[0].Key, clip);
                overrideController.ApplyOverrides(overrides);
                animator.runtimeAnimatorController = overrideController;
                animator.Play("Base", layerIndex, 0f);
            });
        }
    }
}
