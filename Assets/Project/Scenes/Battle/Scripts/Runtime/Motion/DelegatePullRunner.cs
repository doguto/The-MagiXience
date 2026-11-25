using System;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Runtime.Motion
{
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
        public float Time => Elapsed;
        public void Reject() => Rejected = true;

        protected override void OnStart() => onStart?.Invoke(this);
        protected override void OnUpdate(float dt) => onUpdate(this, dt);
    }
}
