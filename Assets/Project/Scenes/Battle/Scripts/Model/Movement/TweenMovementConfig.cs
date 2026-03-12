using System;
using DG.Tweening;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Model.Movement
{
    /// <summary>
    /// DOTween ネイティブの移動設定。
    /// Inspector で targetOffset / duration / Ease を直接指定できる。
    /// isRelative = true のとき targetOffset は現在位置からの相対移動。
    /// isRelative = false のとき targetOffset はワールド座標の絶対位置。
    /// </summary>
    [Serializable]
    public class TweenMovementConfig : IMovementStep
    {
        [SerializeField] Vector3 targetOffset = new Vector3(-3f, 0f, 0f);
        [SerializeField, Min(0.01f)] float duration = 1f;
        [SerializeField] Ease ease = Ease.Linear;
        [SerializeField] bool isRelative = true;

        public Tween Play(Transform target, Vector2 direction, Animator animator)
        {
            Vector3 destination = isRelative ? target.position + targetOffset : targetOffset;
            return target.DOMove(destination, duration).SetEase(ease);
        }
    }
}
