using System;
using System.Collections.Generic;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.BehaviorTree
{
    // 発射位置の半径は 1 発目から最後へ線形に変わる
    [Serializable]
    public sealed class BurstPattern : ShotPattern
    {
        [SerializeField, Min(1)] int wayCount = 5;
        [SerializeField, Tooltip("隣り合う弾の角度 (度)")] float spreadAngle = 72f;
        [SerializeField, Min(1)] int shotCount = 3;
        [SerializeField, Min(0)] int intervalFrames = 6;
        [SerializeField] float startRadius;
        [SerializeField] float endRadius;

        public override void Build(Vector2 baseDirection, List<Shot> shots)
        {
            var baseAngle = Angle(baseDirection);
            for (var k = 0; k < shotCount; k++)
            {
                var radius = shotCount == 1 ? startRadius : Mathf.Lerp(startRadius, endRadius, k / (float)(shotCount - 1));
                for (var i = 0; i < wayCount; i++)
                {
                    var dir = wayCount == 1 ? Rotate(baseAngle) : Rotate(FanAngle(baseAngle, wayCount, spreadAngle, i));
                    shots.Add(new Shot { Direction = dir, Offset = dir * radius, SpawnDelayFrames = k * intervalFrames });
                }
            }
        }

        public override string Summary => $"{wayCount}way {spreadAngle:0.#}° × {shotCount}連 / {intervalFrames}f";
    }
}
