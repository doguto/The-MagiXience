using System;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Runtime.Motion
{
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
}
