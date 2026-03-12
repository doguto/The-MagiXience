using System;
using DG.Tweening;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Model.Movement
{
    /// <summary>
    /// 指定した時間その場で停止する移動ステップ。
    /// </summary>
    [Serializable]
    public class WaitMovementConfig : IMovementStep
    {
        [SerializeField, Min(0f)] float duration = 1f;

        public Tween Play(Transform target, Vector2 direction, Animator animator)
        {
            // 位置を変化させずに duration 秒待機するだけの Tween
            return DOVirtual.DelayedCall(duration, () => { });
        }
    }
}
