using System;
using System.Collections.Generic;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Runtime.Motion
{
    // 移動の対象。弾は風補正前の「本来の軌道」、敵/ボスは Transform そのもの。
    public interface IMotionBody
    {
        Vector3 Position { get; set; }
        Quaternion Rotation { get; set; }
        Transform Transform { get; }
    }

    public sealed class TransformBody : IMotionBody
    {
        public TransformBody(Transform transform) => Transform = transform;
        public Transform Transform { get; }

        public Vector3 Position
        {
            get => Transform.position;
            set => Transform.position = value;
        }

        public Quaternion Rotation
        {
            get => Transform.rotation;
            set => Transform.rotation = value;
        }
    }

    public struct MotionLaunch
    {
        // 発射方向。移動によって使う/使わないが異なる (旧 IMovementStep の direction 引数と同じ)
        public Vector2 Direction;
        // 攻撃の発射時刻 (秒)。頂点の揺らぎなど発射時刻で変わる移動が使う
        public float AttackTime;
    }

    [Serializable]
    public abstract class MotionStep
    {
        public abstract MotionRunner CreateRunner();
        public virtual bool GrantsInvincibility => false;
        public virtual string Summary => null;
    }

    // MotionStep の実行状態。1 回の移動ごとに生成する。
    public abstract class MotionRunner
    {
        protected IMotionBody Body { get; private set; }
        protected Vector2 Direction { get; private set; }
        protected float AttackTime { get; private set; }

        // 開始できない設定 (頂点が発射位置と同じ放物線など)。弾ならその場で消す
        public bool Rejected { get; protected set; }
        public virtual bool GrantsInvincibility => false;

        public void Begin(IMotionBody body, MotionLaunch launch)
        {
            Body = body;
            Direction = launch.Direction;
            AttackTime = launch.AttackTime;
            OnBegin();
        }

        protected virtual void OnBegin() { }

        // dt 秒進める。完了したら true
        public abstract bool Tick(float dt);

        public virtual void Stop() { }
    }

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
