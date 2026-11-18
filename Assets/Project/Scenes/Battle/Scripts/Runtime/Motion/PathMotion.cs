using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Project.Scenes.Battle.Scripts.Runtime.Motion
{
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
}
