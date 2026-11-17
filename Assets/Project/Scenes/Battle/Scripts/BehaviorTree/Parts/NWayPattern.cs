using System;
using System.Collections.Generic;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.BehaviorTree
{
    // 旧 NWaySignal
    [Serializable]
    public sealed class NWayPattern : ShotPattern
    {
        [SerializeField, Min(1)] int wayCount = 3;
        [SerializeField, Tooltip("隣り合う弾の角度 (度)")] float spreadAngle = 60f;
        [SerializeField, Tooltip("発射位置を方向へずらす距離")] float radius;
        [SerializeField, Min(0), Tooltip("動き出すまでのフレーム数")] int startDelayFrames;
        [SerializeField, Tooltip("基準方向からの回転 (度, 反時計回り)")] float rotationOffset;

        public override void Build(Vector2 baseDirection, List<Shot> shots)
        {
            var baseAngle = Angle(baseDirection) + rotationOffset;
            for (var i = 0; i < wayCount; i++)
            {
                var dir = wayCount == 1 ? Rotate(baseAngle) : Rotate(FanAngle(baseAngle, wayCount, spreadAngle, i));
                shots.Add(new Shot
                {
                    Direction = dir,
                    Offset = radius != 0f ? dir * radius : Vector2.zero,
                    StartDelayFrames = startDelayFrames,
                });
            }
        }

        public override string Summary => $"{wayCount}way {spreadAngle:0.#}°";
    }
}
