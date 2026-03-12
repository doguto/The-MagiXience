using DG.Tweening;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Model.Movement
{
    /// <summary>
    /// Tweenベースの移動ステップ。
    /// Play() を呼ぶと Tween を生成して返す。DOTween.Sequence の Append に渡せる。
    /// direction: 自機狙いなど外部から方向を渡す場合に使用。不要なら Vector2.zero。
    /// animator: AnimationMovementConfig で必要。不要なら null。
    /// </summary>
    public interface IMovementStep
    {
        Tween Play(Transform target, Vector2 direction, Animator animator);
    }
}
