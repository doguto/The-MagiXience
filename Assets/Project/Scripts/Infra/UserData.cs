using UnityEngine;

namespace Project.Scripts.Infra
{
    public class UserData
    {
        // 音量のデフォルト値(0-100)。UserData.json未保存時に使用する。
        public const int DefaultVolume = 70;
        // シナリオ文字送り速度(1秒あたりの表示文字数)のデフォルト値と範囲
        public const float DefaultTextSpeed = 25f;
        public const float MinTextSpeed = 1f;
        public const float MaxTextSpeed = 200f;

        public int clearedStageNumber;
        public KeyConfigData keyConfigData;
        public int bgmVolume;
        public int seVolume;
        // 1面に一度でも入ったことがあるか（初回チュートリアルのスキップ待ち判定に使用）
        public bool hasEnteredStage1;
        public float textSpeed;

        public UserData()
        {
            clearedStageNumber = 0;
            keyConfigData = new KeyConfigData();
            bgmVolume = DefaultVolume;
            seVolume = DefaultVolume;
            hasEnteredStage1 = false;
            textSpeed = DefaultTextSpeed;
        }

        // 読み込んだ値を正常な範囲へ補正する。不正なJSON(欠損・型不一致)はJsonUtilityにより
        // 既定値で初期化されたまま残るため、ここでは数値範囲の丸めのみを行う。
        public void Validate()
        {
            // DEMO: Clampの値で公開情報を管理する。Clampのmaxの面まで情報を出す
            clearedStageNumber = Mathf.Clamp(clearedStageNumber, 0, 4);
            bgmVolume = Mathf.Clamp(bgmVolume, 0, 100);
            seVolume = Mathf.Clamp(seVolume, 0, 100);
            textSpeed = ClampTextSpeed(textSpeed);
        }

        public static float ClampTextSpeed(float textSpeed)
        {
            // Mathf.ClampはNaN/Infinityを素通しするため、不正値は既定値に戻す
            if (float.IsNaN(textSpeed) || float.IsInfinity(textSpeed)) return DefaultTextSpeed;
            return Mathf.Clamp(textSpeed, MinTextSpeed, MaxTextSpeed);
        }
    }
}
