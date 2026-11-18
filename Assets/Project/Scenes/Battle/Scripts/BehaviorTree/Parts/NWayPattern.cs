using System;
using System.Collections.Generic;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.BehaviorTree
{
    // 旧 NWaySignal
    [Serializable]
    public sealed class NWayPattern : ShotPattern
    {
        [SerializeField] BtInt wayCount = 3;
        [SerializeField, Tooltip("隣り合う弾の角度 (度)")] BtFloat spreadAngle = 60f;
        [SerializeField, Tooltip("発射位置を方向へずらす距離")] float radius;
        [SerializeField, Min(0), Tooltip("動き出すまでのフレーム数")] int startDelayFrames;
        [SerializeField, Tooltip("基準方向からの回転 (度, 反時計回り)")] float rotationOffset;

        public override void Build(Vector2 baseDirection, List<Shot> shots)
        {
            var baseAngle = Angle(baseDirection) + rotationOffset;
            var count = Mathf.Max(1, wayCount.value);
            for (var i = 0; i < count; i++)
            {
                var dir = count == 1 ? Rotate(baseAngle) : Rotate(FanAngle(baseAngle, count, spreadAngle.value, i));
                shots.Add(new Shot
                {
                    Direction = dir,
                    Offset = radius != 0f ? dir * radius : Vector2.zero,
                    StartDelayFrames = startDelayFrames,
                });
            }
        }

        public override string Summary => $"{wayCount} way {spreadAngle}°";

        public override ShotPattern Resolve(TreeScope scope)
        {
            if (string.IsNullOrEmpty(wayCount.param) && string.IsNullOrEmpty(spreadAngle.param)) return this;
            var copy = (NWayPattern)MemberwiseClone();
            copy.wayCount = wayCount.Resolve(scope);
            copy.spreadAngle = spreadAngle.Resolve(scope);
            return copy;
        }
    }
}
