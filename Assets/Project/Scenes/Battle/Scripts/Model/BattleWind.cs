using System;
using System.Collections.Generic;
using Project.Scripts.Extensions;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Model
{
    public enum WindDirection { None, Up, Down, Left, Right }

    [Serializable]
    public class WindInterval
    {
        public WindDirection direction;
        [Min(0.1f)] public float seconds = 6f;
    }

    [Serializable]
    public class BattleWindSettings
    {
        [Tooltip("自機への風の影響と風向き表示を有効にする。OFFでも弾用の方向スケジュールは継続する。")]
        public bool enabled;
        [Tooltip("無風から風ありに切り替わるときに一度再生する効果音。Noneなら再生しない。")]
        public SeType startSe;
        [Range(0f, 1f)] public float playerSpeedRatio = 0.2f;
        public List<WindInterval> intervals = new();
        [Min(0f), Tooltip("敵弾に与える風の加速度（ワールド単位/秒²）")]
        public float bulletAcceleration = 3f;
        [Min(0f), Tooltip("敵弾の速度に比例する抵抗係数（1/秒）。直進弾の終端速度は加速度÷この値。0なら抵抗なし")]
        public float bulletDrag = 0.6f;
    }
}
