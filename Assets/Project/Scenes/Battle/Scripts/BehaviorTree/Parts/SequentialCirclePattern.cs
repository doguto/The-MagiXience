using System;
using System.Collections.Generic;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.BehaviorTree
{
    // 旧 SequentialCircleBulletSignal
    [Serializable]
    public sealed class SequentialCirclePattern : ShotPattern
    {
        [SerializeField, Min(1)] int wayCount = 12;
        [SerializeField] float radius = 1.5f;
        [SerializeField, Min(0)] int spawnIntervalFrames = 5;
        [SerializeField, Min(0)] int releaseDelayFrames = 12;

        public override void Build(Vector2 baseDirection, List<Shot> shots)
        {
            var baseAngle = Angle(baseDirection);
            for (var i = 0; i < wayCount; i++)
            {
                var dir = Rotate(baseAngle + 360f / wayCount * i);
                shots.Add(new Shot
                {
                    Direction = dir,
                    Offset = dir * radius,
                    SpawnDelayFrames = i * spawnIntervalFrames,
                    StartDelayFrames = (wayCount - 1 - i) * spawnIntervalFrames + releaseDelayFrames,
                });
            }
        }

        public override string Summary => $"順に円 {wayCount}発 / 半径 {radius:0.##}";
    }
}
