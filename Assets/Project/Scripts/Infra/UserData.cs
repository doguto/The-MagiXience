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

        public UserData()
        {
            clearedStageNumber = 0;
            keyConfigData = new KeyConfigData();
            bgmVolume = DefaultVolume;
            seVolume = DefaultVolume;
        }
    }
}
