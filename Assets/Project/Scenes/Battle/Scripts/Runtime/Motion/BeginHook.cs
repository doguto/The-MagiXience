using System;
using DG.Tweening;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Project.Scenes.Battle.Scripts.Runtime.Motion
{
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
