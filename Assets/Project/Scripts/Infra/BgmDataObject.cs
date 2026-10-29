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

        [Tooltip("ゲーム内で表示する曲名。空の場合はnameを使用。")]
        public string displayName;

        public SceneType sceneType;
        public BgmType bgmType;

        [Tooltip("ループ開始位置（サンプル数）。イントロ終了地点。")]
        public int loopStartSamples;

        [Tooltip("ループ終了位置（サンプル数）。0の場合はクリップ全体の長さを使用。")]
        public int loopEndSamples;

        [Header("Sound Room")]
        [Tooltip("サウンドルームで解放されるのに必要なクリア済みステージ数。0なら最初から解放。")]
        public int soundRoomUnlockClearedStage;

        [Tooltip("サウンドルームで表示する説明文（3〜4行程度）。")]
        [TextArea(3, 5)]
        public string description;
    }
}
