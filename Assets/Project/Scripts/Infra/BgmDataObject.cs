using System;
using System.Collections.Generic;
using Project.Scripts.Extensions;
using UnityEngine;

namespace Project.Scripts.Infra
{
    [CreateAssetMenu(fileName = "BgmData", menuName = "Database/BgmData")]
    public class BgmDataObject : ScriptableObject
    {
        public List<BgmData> bgmData = new();
    }

    [Serializable]
    public class BgmData
    {
        // 曲名を使いたいこともある気がするので、Nameも持つ
        public string name;

        public SceneType sceneType;
        public BgmType bgmType;

        [Tooltip("ループ開始位置（サンプル数）。イントロ終了地点。")]
        public int loopStartSamples;

        [Tooltip("ループ終了位置（サンプル数）。0の場合はクリップ全体の長さを使用。")]
        public int loopEndSamples;
    }
}
