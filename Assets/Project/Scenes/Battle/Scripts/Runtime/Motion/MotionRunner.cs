using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Runtime.Motion
{
    // MotionStep の実行状態。1 回の移動ごとに生成する。
    public abstract class MotionRunner
    {
        protected IMotionBody Body { get; private set; }
        protected Vector2 Direction { get; private set; }
        protected float AttackTime { get; private set; }
        protected Vector2 WindAxis { get; private set; }

        // 開始できない設定 (頂点が発射位置と同じ放物線など)。弾ならその場で消す
        public bool Rejected { get; protected set; }
        public virtual bool GrantsInvincibility => false;

        public void Begin(IMotionBody body, MotionLaunch launch)
        {
            Body = body;
            Direction = launch.Direction;
            AttackTime = launch.AttackTime;
            WindAxis = launch.WindAxis;
            OnBegin();
        }

        protected virtual void OnBegin() { }

        // dt 秒進める。完了したら true
        public abstract bool Tick(float dt);

        public virtual void Stop() { }
    }
}
