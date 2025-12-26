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
        public string title;
    }
}
