using System;
using System.Collections.Generic;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Runtime.Motion
{
    // 移動ステップの列を順番に実行する。完了したフレームのうちに次のステップを開始する。
    public sealed class MotionSequence
    {
        readonly IReadOnlyList<MotionStep> steps;
        readonly IMotionBody body;
        readonly MotionLaunch launch;
        int index = -1;
        MotionRunner current;

        public MotionSequence(IReadOnlyList<MotionStep> steps, IMotionBody body, MotionLaunch launch)
        {
            this.steps = steps ?? Array.Empty<MotionStep>();
            this.body = body;
            this.launch = launch;
        }

        public bool IsFinished { get; private set; }
        public bool Rejected { get; private set; }
        public bool GrantsInvincibility => current != null && current.GrantsInvincibility;

        public void Tick(float dt)
        {
            if (IsFinished) return;
            if (current == null && !MoveNext()) return;

            if (!current.Tick(dt)) return;
            MoveNext();
        }

        public void Stop()
        {
            current?.Stop();
            current = null;
            IsFinished = true;
        }

        bool MoveNext()
        {
            while (++index < steps.Count)
            {
                var step = steps[index];
                if (step == null) continue;

                current = step.CreateRunner();
                current.Begin(body, launch);
                if (current.Rejected)
                {
                    Rejected = true;
                    break;
                }
                return true;
            }

            current = null;
            IsFinished = true;
            return false;
        }
    }
}
