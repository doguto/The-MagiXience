using System;
using System.Collections.Generic;
using UnityEngine;

namespace Project.Scripts.Infra
{
    [CreateAssetMenu(fileName = "StageData", menuName = "Database/StageData")]
    public class StageDataObject : ScriptableObject
    {
        public List<StageData> stageData;
    }

    [Serializable]
    public class StageData
    {
        public string id;
        public int stageNumber;  // 文字だと扱いづらいので、int型のステージ番号も持つ
        public string charaStillAddress;
        public string backgroundAddress;
        public string title;
        [Header("Battle Flow")]
        public string waySequenceAddress;
        public string bossSequenceAddress;
        [Header("Scenario")]
        [Tooltip("シナリオ中の敵立ち絵の表示位置の補正(UI座標)。立ち絵ごとに顔の位置などが違うのを揃えるために使う")]
        public Vector2 scenarioEnemyStillOffset;
        [Tooltip("シナリオ中の敵立ち絵の拡大率。0以下は1倍として扱う")]
        public float scenarioEnemyStillScale = 1f;
    }
}
