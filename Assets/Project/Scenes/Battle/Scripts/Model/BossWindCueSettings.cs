using System;
using System.Collections.Generic;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Model
{
    // BossWindCue ノードの設定。腕上げの合図のあと風向きを切り替える
    [Serializable]
    public class BossWindCueSettings
    {
        public bool enabled;
        public WindDirection direction = WindDirection.Up;
        [Min(0f), Tooltip("腕上げ中は前の風を保ち、この秒数後に切り替える。")]
        public float cueSeconds = 0.9f;
        [Range(0f, 1f)] public float playerSpeedRatio = 0.2f;
        [Min(0f)] public float bulletAcceleration = 1.5f;
        [Min(0f)] public float bulletDrag = 0.25f;
        [Tooltip("腕上げ中のスケール変化率。")]
        public Vector2 cueScale = new(-0.04f, 0.08f);

        public BattleWindSettings CreateWind(WindDirection previousDirection)
        {
            var firstDirection = previousDirection == WindDirection.None ? direction : previousDirection;
            var intervals = new List<WindInterval>();
            if (cueSeconds > 0f)
                intervals.Add(new WindInterval { direction = firstDirection, seconds = cueSeconds });
            intervals.Add(new WindInterval { direction = direction, seconds = 3600f });
            return new BattleWindSettings
            {
                enabled = true, playerSpeedRatio = playerSpeedRatio,
                bulletAcceleration = bulletAcceleration, bulletDrag = bulletDrag, intervals = intervals
            };
        }
    }
}
