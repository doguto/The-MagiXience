using System;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Runtime.Motion
{
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
}
