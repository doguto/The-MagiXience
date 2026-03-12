using System;
using DG.Tweening;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Model.Movement
{
    /// <summary>
    /// 複数のウェイポイントを順番に通過する移動ステップ。
    /// PathType.CatmullRom で滑らかな曲線、Linear で折れ線になる。
    /// ウェイポイントは現在位置からの相対座標で指定する。
    /// </summary>
    [Serializable]
    public class PathMovementConfig : IMovementStep
    {
        [SerializeField] Vector3[] waypoints = { new Vector3(-3f, 0f, 0f) };
        [SerializeField, Min(0.01f)] float duration = 1f;
        [SerializeField] PathType pathType = PathType.CatmullRom;
        [SerializeField] Ease ease = Ease.Linear;
        [SerializeField] bool isRelative = true;

        public Tween Play(Transform target, Vector2 direction, Animator animator)
        {
            Vector3[] resolvedWaypoints;
            if (isRelative)
            {
                resolvedWaypoints = new Vector3[waypoints.Length];
                for (int i = 0; i < waypoints.Length; i++)
                    resolvedWaypoints[i] = target.position + waypoints[i];
            }
            else
            {
                resolvedWaypoints = waypoints;
            }

            return target.DOPath(resolvedWaypoints, duration, pathType, PathMode.Sidescroller2D)
                .SetEase(ease);
        }
    }
}
