using System;
using DG.Tweening;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Model.Movement
{
    /// <summary>
    /// 指定した時間その場で停止しつつ、Entityを無敵状態にする移動ステップ。
    /// 無敵の切り替え自体はPresenter側がIInvincibilityGrantingStepを見て行う。
    /// </summary>
    [Serializable]
    public class InvincibleWaitMovementConfig : IMovementStep, IInvincibilityGrantingStep
    {
        [SerializeField, Min(0f)] float duration = 1f;

        public Tween Play(Transform target, Vector2 direction, Animator animator)
        {
            return DOVirtual.DelayedCall(duration, () => { });
        }
    }
}
