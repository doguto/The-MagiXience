using System;
using System.Collections.Generic;
using Project.Scripts.Extensions;
using UnityEngine;

namespace Project.Scripts.Infra
{
    [CreateAssetMenu(fileName = "SeData", menuName = "Database/SeData")]
    public class SeDataObject : ScriptableObject
    {
        public List<SeData> seData = new();
    }

    [Serializable]
    public class SeData
    {
        public string name;
        public SeType seType;
        // ループ再生用（PlayLoopSE時のみ参照される）。0 の場合はクリップ末尾までを使う。
        // 厳密にはAsset分けた方が良さそうだけど、暫定的にこれで
        public int loopStartSamples;
        public int loopEndSamples;
    }
}
