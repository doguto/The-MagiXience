using UnityEngine;

namespace Project.Scripts.Infra
{
    public class UserData
    {
        // 音量のデフォルト値(0-100)。UserData.json未保存時に使用する。
        public const int DefaultVolume = 70;

        public int clearedStageNumber;
        public KeyConfigData keyConfigData;
        public int bgmVolume;
        public int seVolume;
        // 1面に一度でも入ったことがあるか（初回チュートリアルのスキップ待ち判定に使用）
        public bool hasEnteredStage1;

        public UserData()
        {
            clearedStageNumber = 0;
            keyConfigData = new KeyConfigData();
            bgmVolume = DefaultVolume;
            seVolume = DefaultVolume;
            hasEnteredStage1 = false;
        }

        // 読み込んだ値を正常な範囲へ補正する。不正なJSON(欠損・型不一致)はJsonUtilityにより
        // 既定値で初期化されたまま残るため、ここでは数値範囲の丸めのみを行う。
        public void Validate()
        {
            // clearedStageNumberは0か1のみ有効。それ以外の数値は近い方へ丸める。
            clearedStageNumber = Mathf.Clamp(clearedStageNumber, 0, 1);
            bgmVolume = Mathf.Clamp(bgmVolume, 0, 100);
            seVolume = Mathf.Clamp(seVolume, 0, 100);
        }
    }
}
