using System;
using System.IO;
using Project.Scripts.Extensions;
using Project.Scripts.Infra;
using UnityEngine;

namespace Project.Scripts.Model
{
    public class UserModel : ModelBase
    {
        readonly string saveDirectoryPath;
        readonly string saveFilePath;


        public UserModel()
        {
            saveDirectoryPath = Path.Combine(Application.persistentDataPath, "DataStore");
#if UNITY_EDITOR
            saveDirectoryPath = Path.Combine("Assets", "Project", "DataStore");
#endif
            saveFilePath = Path.Combine(saveDirectoryPath, "UserData.json");
            UserData = Load();
        }

        public UserData UserData { get; set; }

        public int ClearedStageNumber => UserData.clearedStageNumber;
        public int OpenedStageNumber => UserData.clearedStageNumber - 1;

        // このステージをクリアするとタイトルのBGMが変化する
        const int TitleBgmChangedStageNumber = 4;

        // タイトルで再生するBGMの種類。ステージ4クリア後は Phase2 の楽曲へ切り替わる。
        public BgmType TitleBgmType
            => IsClearedStage(TitleBgmChangedStageNumber) ? BgmType.Phase2 : BgmType.Default;

        public int BgmVolume => UserData.bgmVolume;
        public int SeVolume => UserData.seVolume;

        // 音量をメモリ上に反映する。永続化はSave()を別途呼ぶ(Optionモーダルを閉じたタイミング等)。
        public void SetVolume(int bgmVolume, int seVolume)
        {
            UserData.bgmVolume = Mathf.Clamp(bgmVolume, 0, 100);
            UserData.seVolume = Mathf.Clamp(seVolume, 0, 100);
        }

        // シナリオの文字送り速度(1秒あたりの表示文字数)。TypeWriterコマンドでdefault指定時に使用する
        public float TextSpeed => UserData.textSpeed;

        // 送り速度をメモリ上に反映する。永続化はSave()を別途呼ぶ。
        public void SetTextSpeed(float textSpeed)
        {
            UserData.textSpeed = UserData.ClampTextSpeed(textSpeed);
        }

        public void StageClear(int stageNumber, StageClearRank rank)
        {
            UserData.clearedStageNumber = Math.Max(UserData.clearedStageNumber, stageNumber);

            // クリアランクは最高記録のみ保持する
            var index = stageNumber - 1;
            if (index >= 0)
            {
                if (UserData.stageClearRanks.Length <= index)
                {
                    Array.Resize(ref UserData.stageClearRanks, index + 1);
                }
                UserData.stageClearRanks[index] = Math.Max(UserData.stageClearRanks[index], (int)rank);
            }
            Save();
        }

        // 記録が無いがクリア済みのステージ(ランク記録導入前のセーブ)はNormal扱いにする
        public StageClearRank GetStageClearRank(int stageNumber)
        {
            if (!IsClearedStage(stageNumber)) return StageClearRank.None;

            var index = stageNumber - 1;
            var rank = index >= 0 && index < UserData.stageClearRanks.Length
                ? (StageClearRank)UserData.stageClearRanks[index]
                : StageClearRank.None;
            return rank == StageClearRank.None ? StageClearRank.Normal : rank;
        }

        public bool HasEnteredStage1 => UserData.hasEnteredStage1;

        public void MarkEnteredStage1()
        {
            if (UserData.hasEnteredStage1) return;
            UserData.hasEnteredStage1 = true;
            Save();
        }

        public bool IsClearedStage(int stageNumber)
        {
            return UserData.clearedStageNumber >= stageNumber;
        }

        public bool IsOpenedStage(int stageNumber)
        {
            return UserData.clearedStageNumber >= stageNumber - 1;
        }

        public UserData Load()
        {
            if (!File.Exists(saveFilePath) && !File.Exists(BackupFilePath))
            {
                UserData = new UserData();
                Save();
                return UserData;
            }

            if (TryLoadFrom(saveFilePath, out var data)) return data;

            // 本体が壊れている場合はバックアップから復旧を試みる。
            // 壊れたファイルは次回Save()で上書きされないよう退避しておく。
            QuarantineCorruptFile();
            if (TryLoadFrom(BackupFilePath, out data)) return data;

            return new UserData();
        }

        string TempFilePath => saveFilePath + ".tmp";
        string BackupFilePath => saveFilePath + ".bak";
        string CorruptFilePath => saveFilePath + ".corrupt";

        bool TryLoadFrom(string path, out UserData data)
        {
            data = null;
            try
            {
                if (!File.Exists(path)) return false;

                var json = File.ReadAllText(path);
                if (string.IsNullOrWhiteSpace(json)) return false;

                // 音量フィールドが無い旧データを読み込むと0になるため、
                // 既定値で初期化したインスタンスへ上書きして欠損フィールドを補完する。
                var loaded = new UserData();
                JsonUtility.FromJsonOverwrite(json, loaded);
                loaded.Validate();
                data = loaded;
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to load user data ({path}): {e.Message}");
                return false;
            }
        }

        void QuarantineCorruptFile()
        {
            try
            {
                if (!File.Exists(saveFilePath)) return;
                if (File.Exists(CorruptFilePath)) File.Delete(CorruptFilePath);
                File.Move(saveFilePath, CorruptFilePath);
            }
            catch (Exception e)
            {
                Debug.LogError("Failed to quarantine corrupt user data: " + e.Message);
            }
        }

        void ReplaceSaveFile()
        {
            if (!File.Exists(saveFilePath))
            {
                File.Move(TempFilePath, saveFilePath);
                return;
            }

            try
            {
                File.Replace(TempFilePath, saveFilePath, BackupFilePath);
            }
            catch (PlatformNotSupportedException)
            {
                // File.Replace非対応のプラットフォーム向け。本体が消えた瞬間に落ちてもLoad()が.bakから復旧する。
                File.Copy(saveFilePath, BackupFilePath, true);
                File.Delete(saveFilePath);
                File.Move(TempFilePath, saveFilePath);
            }
        }

        public void Save()
        {
            try
            {
                if (!Directory.Exists(saveDirectoryPath))
                {
                    Directory.CreateDirectory(saveDirectoryPath);
                }

                var json = JsonUtility.ToJson(UserData, true);

                // 書き込み途中のクラッシュでセーブが破損しないよう、一時ファイルへ書いてから置き換える。
                File.WriteAllText(TempFilePath, json);
                ReplaceSaveFile();

#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.Log("Saved user data");
#endif
            }
            catch (Exception e)
            {
                Debug.LogError("Failed to save user data: " + e.Message);
            }
        }
    }
}
