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

    /// <summary>同じプリセットを使う次のボスフェーズでも、再生中の移動を継続できる。</summary>
    public interface IPhaseContinuousMovementStep
    {
        bool ContinueAcrossPhases { get; }
    }

    /// <summary>発射予定時刻から、共有設定を変更せず今回の弾の軌道を確定する。</summary>
    public interface IAttackTimeMovementStep
    {
        IMovementStep SampleForAttackTime(float attackTime);
    }
}
