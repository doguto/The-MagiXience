using System;
using System.Collections.Generic;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.BehaviorTree
{
    // 旧 SequentialFanBulletSignal
    [Serializable]
    public sealed class SequentialFanPattern : ShotPattern
    {
        [SerializeField, Min(1)] int wayCount = 13;
        [SerializeField] float spreadAngle = 15f;
        [SerializeField, Min(0)] int spawnIntervalFrames = 6;
        [SerializeField, Min(0), Tooltip("最後の 1 発を出してから動き出すまで")] int releaseDelayFrames = 18;

        public override void Build(Vector2 baseDirection, List<Shot> shots)
        {
            var baseAngle = Angle(baseDirection);
            for (var i = 0; i < wayCount; i++)
            {
                shots.Add(new Shot
                {
                    Direction = wayCount == 1 ? baseDirection : Rotate(FanAngle(baseAngle, wayCount, spreadAngle, i)),
                    SpawnDelayFrames = i * spawnIntervalFrames,
                    StartDelayFrames = (wayCount - 1 - i) * spawnIntervalFrames + releaseDelayFrames,
                });
            }
        }

        public override string Summary => $"順に扇 {wayCount}発 / {spawnIntervalFrames}f 間隔";
    }
}
