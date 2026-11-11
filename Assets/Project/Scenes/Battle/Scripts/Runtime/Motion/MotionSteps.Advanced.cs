using System;
using System.Collections.Generic;
using DG.Tweening;
using Project.Scenes.Battle.Scripts.Model;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Project.Scenes.Battle.Scripts.Runtime.Motion
{
    // 旧 AcceleratedMovementConfig。通常 / ワールド加速度 / 自機狙い放物線 の 3 モード
    [Serializable]
    public sealed class AcceleratedMotion : MotionStep
    {
        [SerializeField] Vector2 direction = Vector2.left;
        [SerializeField, Tooltip("ON で発射方向より Direction を優先する")] bool useConfiguredDirection;
        [SerializeField, Min(0f)] float initialSpeed = 2f;
        [SerializeField, Tooltip("負の値で減速")] float acceleration = 1f;
        [SerializeField, Min(0f)] float maxSpeed = 10f;
        [SerializeField, Min(0f)] float minSpeed;
        [SerializeField, Min(0f), Tooltip("0 で無限")] float duration;
        [SerializeField, Tooltip("固定方向の加速度で放物運動する")] bool useWorldAcceleration;
        [SerializeField] Vector2 worldAcceleration = Vector2.down;

        [Header("Aimed parabola")]
        [SerializeField] bool aimAtPlayer;
        [SerializeField] bool useBattleWind = true;
        [SerializeField, Min(0.1f)] float targetFlightTime = 4f;
        [SerializeField, Min(0.001f)] float vertexDepth = 1.1f;
        [SerializeField, Min(0f)] float vertexScreenMargin = 0.35f;
        [SerializeField, Min(0)] int rightShotInterval;
        [SerializeField] bool alternateVertical;
        [SerializeField, Range(0f, 0.4f)] float flightTimeVariation;
        [SerializeField, Range(0f, 0.8f)] float vertexDepthVariation;

        public override string Summary => aimAtPlayer ? $"自機狙い放物線 {targetFlightTime:0.##}s"
            : useWorldAcceleration ? $"放物運動 初速 {initialSpeed:0.##}" : $"加速 {initialSpeed:0.##} → {acceleration:+0.##;-0.##}/s";

        public override MotionRunner CreateRunner()
        {
            if (aimAtPlayer) return CreateAimed();

            var v = Vector3.zero;
            var dir = Vector3.zero;
            return new DelegatePullRunner(duration, false, (r, dt) =>
            {
                if (useWorldAcceleration)
                {
                    Vector3 g = worldAcceleration;
                    r.Target.Position += v * dt + 0.5f * g * dt * dt;
                    v += g * dt;
                    return;
                }
                v += dir * acceleration * dt;
                var speed = v.magnitude;
                if (maxSpeed > 0f && speed > maxSpeed) v = v.normalized * maxSpeed;
                if (speed < minSpeed) v = speed > 0f ? v.normalized * minSpeed : dir * minSpeed;
                r.Target.Position += v * dt;
            }, r =>
            {
                dir = ((Vector3)(!useConfiguredDirection && r.LaunchDirection != Vector2.zero ? r.LaunchDirection : direction)).normalized;
                v = dir * initialSpeed;
            });
        }

        MotionRunner CreateAimed()
        {
            Vector2 start = default, velocity = default, accel = default;
            var z = 0f;
            var aimed = false;
            return new DelegatePullRunner(duration, false, (r, _) =>
            {
                if (!aimed) return;
                var p = Kinematics.Position(start, velocity, accel, r.Time);
                r.Target.Position = new Vector3(p.x, p.y, z);
            }, r =>
            {
                if (!BattleTargets.TryGetPlayerPosition(out var player)) return;
                aimed = true;
                start = r.Target.Position;
                z = r.Target.Position.z;
                Vector2 axis = useBattleWind ? BattleWind.ProjectileVector : worldAcceleration.normalized;
                var shot = AimedParabola.NextShotIndex();
                if (alternateVertical) axis = shot % 2 == 0 ? Vector2.up : Vector2.down;
                var flightTime = Mathf.Max(0.1f, targetFlightTime * (1f + flightTimeVariation * ((shot % 11) / 5f - 1f)));
                var depth = Mathf.Max(0.001f, vertexDepth * (1f + vertexDepthVariation * ((shot % 7) / 3f - 1f)));
                var screen = Rect.MinMaxRect(ScreenBoundsCache.MinX, ScreenBoundsCache.MinY, ScreenBoundsCache.MaxX, ScreenBoundsCache.MaxY);
                AimedParabola.SolveMixed(start, player, axis, screen, flightTime, depth, vertexScreenMargin,
                    rightShotInterval > 0 && shot % rightShotInterval == 0, out velocity, out accel);
            });
        }
    }

    // 旧 VertexParabolaMovementConfig
    [Serializable]
    public sealed class VertexParabolaMotion : MotionStep
    {
        [SerializeField] ParabolaAxis orientation = ParabolaAxis.Vertical;
        [SerializeField, Tooltip("頂点。Is Relative なら発射位置からの相対")] Vector2 vertex = new(-3f, 4.5f);
        [SerializeField, Min(0.01f)] float initialSpeed = 5f;
        [SerializeField] bool isRelative = true;
        [SerializeField, Tooltip("発射時刻で頂点を揺らす")] bool oscillateVertex;
        [SerializeField] Vector2 vertexAmplitude;
        [SerializeField, Tooltip("rad/s")] Vector2 vertexAngularSpeed;
        [SerializeField] float attackTimeOffset;

        public VertexParabolaMotion() { }

        public VertexParabolaMotion(Vector2 vertex, float initialSpeed, bool isRelative, ParabolaAxis orientation = ParabolaAxis.Vertical)
        {
            this.vertex = vertex;
            this.initialSpeed = initialSpeed;
            this.isRelative = isRelative;
            this.orientation = orientation;
        }

        public override string Summary => $"放物線 頂点 {vertex}{(isRelative ? " (相対)" : "")} / 速さ {initialSpeed:0.##}";

        public override MotionRunner CreateRunner()
        {
            Vector2 start = default, velocity = default, accel = default;
            var z = 0f;
            DelegatePullRunner runner = null;
            runner = new DelegatePullRunner(0f, false, (r, _) =>
            {
                var p = Kinematics.Position(start, velocity, accel, r.Time);
                r.Target.Position = new Vector3(p.x, p.y, z);
            });
            return new BeginHook(runner, r =>
            {
                start = r.Target.Position;
                z = r.Target.Position.z;
                var t = Mathf.Max(0f, r.LaunchAttackTime - attackTimeOffset);
                var offset = oscillateVertex
                    ? new Vector2(vertexAmplitude.x * Mathf.Sin(t * vertexAngularSpeed.x), vertexAmplitude.y * Mathf.Sin(t * vertexAngularSpeed.y))
                    : Vector2.zero;
                var v = vertex + offset;
                var worldVertex = isRelative ? start + v : v;
                if (!VertexParabola.TrySolve(start, worldVertex, orientation, initialSpeed, out velocity, out accel))
                {
                    Debug.LogWarning($"[VertexParabolaMotion] Invalid parabola. start={start} vertex={worldVertex} speed={initialSpeed}");
                    r.Reject();
                }
            });
        }
    }

    // 旧 ReflectMovementConfig。一度画面内に入ってから画面端で反射する
    [Serializable]
    public sealed class ReflectMotion : MotionStep
    {
        [SerializeField] float speed = 5f;
        [SerializeField] int maxReflections = 1;
        [SerializeField] bool reflectTop = true;
        [SerializeField] bool reflectBottom = true;
        [SerializeField] bool reflectLeft;
        [SerializeField] bool reflectRight;

        public override string Summary => $"反射 {speed:0.##} / 最大 {maxReflections} 回";

        public override MotionRunner CreateRunner()
        {
            var vel = Vector2.zero;
            var reflections = 0;
            var entered = false;
            float minX = 0f, maxX = 0f, minY = 0f, maxY = 0f;
            var runner = new DelegatePullRunner(0f, true, (r, dt) =>
            {
                var pos = (Vector2)r.Target.Position + vel * dt;
                var inScreen = pos.x >= minX && pos.x <= maxX && pos.y >= minY && pos.y <= maxY;
                if (!entered)
                {
                    entered = inScreen;
                }
                else if (reflections < maxReflections)
                {
                    if (reflectLeft && pos.x <= minX) { vel.x = -vel.x; pos.x = minX; reflections++; }
                    else if (reflectRight && pos.x >= maxX) { vel.x = -vel.x; pos.x = maxX; reflections++; }
                    if (reflectBottom && pos.y <= minY) { vel.y = -vel.y; pos.y = minY; reflections++; }
                    else if (reflectTop && pos.y >= maxY) { vel.y = -vel.y; pos.y = maxY; reflections++; }
                }
                r.Target.Position = new Vector3(pos.x, pos.y, r.Target.Position.z);
            });
            return new BeginHook(runner, r =>
            {
                minX = ScreenBoundsCache.MinX; maxX = ScreenBoundsCache.MaxX;
                minY = ScreenBoundsCache.MinY; maxY = ScreenBoundsCache.MaxY;
                vel = r.LaunchDirection != Vector2.zero ? r.LaunchDirection.normalized * speed : Vector2.left * speed;
            });
        }
    }

    // 旧 ZigzagMovementConfig
    [Serializable]
    public sealed class ZigzagMotion : MotionStep
    {
        [SerializeField, Min(0f)] float speed = 6f;
        [SerializeField, Tooltip("基準方向からの振れ角 (度)")] float angleDegrees = 45f;
        [SerializeField, Min(0.01f), Tooltip("折り返すまでの距離")] float segmentLength = 1f;
        [SerializeField, Tooltip("最初に振れる向き (0以上で +)")] int initialSign = 1;
        [SerializeField, Tooltip("0 で無制限")] int maxReflections;
        [SerializeField, Tooltip("0 で Segment Length と同じ")] float firstSegmentLength;
        [SerializeField, Min(0f), Tooltip("0 で無限")] float duration;
        [SerializeField] Vector2 fallbackAxis = Vector2.left;
        [SerializeField] bool rotateToVelocity;
        [SerializeField] float rotationOffsetDegrees;

        public override string Summary => $"ジグザグ {speed:0.##} / ±{angleDegrees:0.#}°";

        public override MotionRunner CreateRunner()
        {
            float axisAngle = 0f, traveled = 0f, currentSegment = 0f;
            int sign = 1, reflections = 0;
            var velocity = Vector2.zero;

            void ApplyRotation(IMotionBody body)
            {
                if (!rotateToVelocity) return;
                body.Rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg + rotationOffsetDegrees);
            }

            return new DelegatePullRunner(duration, false, (r, dt) =>
            {
                var remaining = speed * dt;
                while (remaining > 0f)
                {
                    var canTurn = maxReflections <= 0 || reflections < maxReflections;
                    var toTurn = canTurn ? currentSegment - traveled : remaining;
                    var move = Mathf.Min(remaining, toTurn);
                    r.Target.Position += (Vector3)(velocity * move);
                    remaining -= move;
                    if (!canTurn) break;
                    traveled += move;
                    if (traveled < currentSegment - Mathf.Epsilon) break;
                    traveled = 0f;
                    currentSegment = segmentLength;
                    sign = -sign;
                    reflections++;
                    velocity = AngleToDirection(axisAngle + angleDegrees * sign);
                    ApplyRotation(r.Target);
                }
            }, r =>
            {
                var axis = r.LaunchDirection != Vector2.zero ? r.LaunchDirection.normalized : fallbackAxis.normalized;
                if (axis == Vector2.zero) axis = Vector2.left;
                axisAngle = Mathf.Atan2(axis.y, axis.x) * Mathf.Rad2Deg;
                sign = initialSign >= 0 ? 1 : -1;
                currentSegment = firstSegmentLength > 0f ? firstSegmentLength : segmentLength;
                velocity = AngleToDirection(axisAngle + angleDegrees * sign);
                ApplyRotation(r.Target);
            });
        }

        static Vector2 AngleToDirection(float degrees)
        {
            var rad = degrees * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
        }
    }

    // 旧 SpiralMovementConfig (ChargeSignal が生成していたもの)。中心の周りを回りながら半径を変える
    [Serializable]
    public sealed class SpiralMotion : MotionStep
    {
        [SerializeField, Min(0f)] float startRadius = 3.5f;
        [SerializeField, Min(0f)] float targetRadius;
        [SerializeField, Min(0.01f)] float duration = 1.2f;
        [SerializeField, Tooltip("度/秒。負で時計回り")] float rotationSpeed = 480f;

        public SpiralMotion() { }

        public SpiralMotion(float startRadius, float targetRadius, float duration, float rotationSpeed)
        {
            this.startRadius = startRadius;
            this.targetRadius = targetRadius;
            this.duration = duration;
            this.rotationSpeed = rotationSpeed;
        }

        public override string Summary => $"渦巻き 半径 {startRadius:0.##}→{targetRadius:0.##} / {duration:0.##}s";

        public override MotionRunner CreateRunner()
        {
            var center = Vector3.zero;
            var angle0 = 0f;
            var d = Mathf.Max(0.01f, duration);
            return new DelegatePullRunner(d, false, (r, _) =>
            {
                var radius = Mathf.Lerp(startRadius, targetRadius, Mathf.Clamp01(r.Time / d));
                var theta = angle0 + rotationSpeed * Mathf.Deg2Rad * r.Time;
                r.Target.Position = center + new Vector3(Mathf.Cos(theta), Mathf.Sin(theta), 0f) * radius;
            }, r =>
            {
                var dir = r.LaunchDirection != Vector2.zero ? r.LaunchDirection.normalized : (Vector2)(r.Target.Rotation * Vector3.right);
                center = r.Target.Position - (Vector3)dir * startRadius;
                angle0 = Mathf.Atan2(dir.y, dir.x);
            });
        }
    }

    // 旧 DriftMovementConfig。anchor の周りを「移動 → 停止」で漂う
    [Serializable]
    public sealed class DriftMotion : MotionStep
    {
        [SerializeField] Vector3 anchor = new(-2f, 0f, 0f);
        [SerializeField] float pullStrength = 0.5f;
        [SerializeField, Min(0.01f)] float moveDistance = 0.5f;
        [SerializeField, Tooltip("移動範囲 (幅, 高さ)")] Vector2 moveBounds = new(3f, 2f);
        [SerializeField, Min(0.01f)] float moveDuration = 0.6f;
        [SerializeField, Min(0f)] float pauseDuration = 0.3f;
        [SerializeField] Ease moveEase = Ease.InOutSine;
        [SerializeField, Min(0f), Tooltip("0 で無限")] float duration;

        public override string Summary => $"{anchor} 周りを漂う";

        public override MotionRunner CreateRunner()
        {
            var cycleDuration = moveDuration + pauseDuration;
            Vector3 moveStart = default, moveEnd = default;
            var cycle = 0f;
            return new DelegatePullRunner(duration, false, (r, dt) =>
            {
                cycle += dt;
                if (cycle >= cycleDuration)
                {
                    r.Target.Position = moveEnd;
                    moveStart = moveEnd;
                    moveEnd = NextTarget(moveStart);
                    cycle -= cycleDuration;
                }
                r.Target.Position = cycle < moveDuration
                    ? Vector3.Lerp(moveStart, moveEnd, MotionEasing.Evaluate(moveEase, cycle / moveDuration))
                    : moveEnd;
            }, r =>
            {
                moveStart = r.Target.Position;
                moveEnd = NextTarget(moveStart);
            });
        }

        Vector3 NextTarget(Vector3 current)
        {
            var toAnchor = anchor - current;
            var pull = toAnchor.normalized * Mathf.Min(pullStrength, toAnchor.magnitude);
            var angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            var combined = pull + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);
            var candidate = current + (combined.sqrMagnitude > 0f ? combined.normalized : Vector3.right) * moveDistance;
            candidate.x = Mathf.Clamp(candidate.x, anchor.x - moveBounds.x * 0.5f, anchor.x + moveBounds.x * 0.5f);
            candidate.y = Mathf.Clamp(candidate.y, anchor.y - moveBounds.y * 0.5f, anchor.y + moveBounds.y * 0.5f);
            return candidate;
        }
    }

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

    // 旧 PathMovementConfig (DOPath Sidescroller2D)。ウェイポイントを等速で通過する
    [Serializable]
    public sealed class PathMotion : MotionStep
    {
        public enum Shape
        {
            CatmullRom,
            Linear,
        }

        [SerializeField] Vector3[] waypoints = { new(-3f, 0f, 0f) };
        [SerializeField, Min(0.01f)] float duration = 1f;
        [SerializeField] Shape shape = Shape.CatmullRom;
        [SerializeField] MotionEase ease = MotionEase.Of(Ease.Linear);
        [SerializeField] bool isRelative = true;

        public override string Summary => $"パス {waypoints?.Length ?? 0} 点 / {duration:0.##}s";

        public override MotionRunner CreateRunner() => new PathRunner(this);

        sealed class PathRunner : MotionRunner
        {
            const int Subdivisions = 10;
            readonly PathMotion def;
            readonly List<Vector3> samples = new();
            readonly List<float> lengths = new();
            float elapsed;

            public PathRunner(PathMotion def) => this.def = def;

            protected override void OnBegin()
            {
                var start = Body.Position;
                var points = new List<Vector3> { start };
                foreach (var wp in def.waypoints ?? Array.Empty<Vector3>())
                {
                    points.Add(def.isRelative ? start + wp : wp);
                }

                samples.Clear();
                lengths.Clear();
                samples.Add(points[0]);
                lengths.Add(0f);
                for (var i = 0; i < points.Count - 1; i++)
                {
                    var p0 = i > 0 ? points[i - 1] : points[i];
                    var p1 = points[i];
                    var p2 = points[i + 1];
                    var p3 = i + 2 < points.Count ? points[i + 2] : points[i + 1];
                    for (var s = 1; s <= Subdivisions; s++)
                    {
                        var t = s / (float)Subdivisions;
                        var p = def.shape == Shape.Linear ? Vector3.Lerp(p1, p2, t) : CatmullRom(p0, p1, p2, p3, t);
                        lengths.Add(lengths[^1] + Vector3.Distance(samples[^1], p));
                        samples.Add(p);
                    }
                }
            }

            public override bool Tick(float dt)
            {
                elapsed += dt;
                var u = def.ease.Evaluate(Mathf.Clamp01(elapsed / def.duration));
                Body.Position = Sample(u);
                return elapsed >= def.duration;
            }

            Vector3 Sample(float u)
            {
                if (samples.Count == 1) return samples[0];
                var target = lengths[^1] * u;
                for (var i = 1; i < samples.Count; i++)
                {
                    if (lengths[i] < target) continue;
                    var seg = lengths[i] - lengths[i - 1];
                    var t = seg > 0f ? (target - lengths[i - 1]) / seg : 0f;
                    return Vector3.LerpUnclamped(samples[i - 1], samples[i], t);
                }
                return samples[^1];
            }

            static Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
            {
                var t2 = t * t;
                var t3 = t2 * t;
                return 0.5f * (2f * p1 + (-p0 + p2) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 + (-p0 + 3f * p1 - 3f * p2 + p3) * t3);
            }
        }
    }

    // 旧 LoopMovementConfig。エントリ (プリセット or インライン) を順に繰り返す
    [Serializable]
    public sealed class LoopMotion : MotionStep
    {
        [Serializable]
        public sealed class Entry
        {
            [Tooltip("指定するとインラインより優先")] public MotionPreset preset;
            [SerializeReference, SubclassSelector] public List<MotionStep> inlineSteps = new();
            [Min(1)] public int repeatCount = 1;

            public IReadOnlyList<MotionStep> Steps => preset != null ? preset.Steps : inlineSteps;
        }

        [SerializeField] bool loop = true;
        [SerializeField, Tooltip("0 で無限")] int loopCount;
        [SerializeField] List<Entry> entries = new();

        public override string Summary => loop ? (loopCount > 0 ? $"ループ ×{loopCount}" : "ループ ∞") : "1 周";

        public override MotionRunner CreateRunner() => new LoopRunner(this);

        sealed class LoopRunner : MotionRunner
        {
            readonly LoopMotion def;
            readonly List<IReadOnlyList<MotionStep>> playlist = new();
            int index;
            int pass;
            MotionSequence current;

            public LoopRunner(LoopMotion def) => this.def = def;

            protected override void OnBegin()
            {
                foreach (var entry in def.entries)
                {
                    if (entry == null) continue;
                    for (var i = 0; i < Mathf.Max(1, entry.repeatCount); i++) playlist.Add(entry.Steps);
                }
            }

            public override bool Tick(float dt)
            {
                if (playlist.Count == 0) return true;

                // 1 フレームで空のエントリを延々回さないよう、進める回数に上限を設ける
                for (var guard = 0; guard <= playlist.Count; guard++)
                {
                    current ??= new MotionSequence(playlist[index], Body, new MotionLaunch { Direction = Direction, AttackTime = AttackTime });
                    current.Tick(dt);
                    if (!current.IsFinished) return false;

                    current = null;
                    dt = 0f;
                    if (++index < playlist.Count) continue;

                    index = 0;
                    pass++;
                    if (!def.loop || (def.loopCount > 0 && pass >= def.loopCount)) return true;
                }
                return false;
            }

            public override void Stop() => current?.Stop();
        }
    }

    // OnBegin 時点で初期化が必要な移動 (開始できないと判定したら Rejected にする) 用のラッパー
    sealed class BeginHook : MotionRunner
    {
        readonly DelegatePullRunner inner;
        readonly Action<DelegatePullRunner> onBegin;

        public BeginHook(DelegatePullRunner inner, Action<DelegatePullRunner> onBegin)
        {
            this.inner = inner;
            this.onBegin = onBegin;
        }

        public override bool GrantsInvincibility => inner.GrantsInvincibility;

        protected override void OnBegin()
        {
            inner.Begin(Body, new MotionLaunch { Direction = Direction, AttackTime = AttackTime });
            onBegin(inner);
            Rejected = inner.Rejected;
        }

        public override bool Tick(float dt) => inner.Tick(dt);
        public override void Stop() => inner.Stop();
    }
}
