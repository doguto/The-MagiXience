using System;
using System.IO;
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
            Debug.Log(saveDirectoryPath);
            saveFilePath = Path.Combine(saveDirectoryPath, "UserData.json");
            UserData = Load();
        }

        public UserData UserData { get; set; }

        public int ClearedStageNumber => UserData.clearedStageNumber;
        public int OpenedStageNumber => UserData.clearedStageNumber - 1;

        public void StageClear(int stageNumber)
        {
            UserData.clearedStageNumber = Math.Max(UserData.clearedStageNumber, stageNumber);
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
            if (!File.Exists(saveFilePath))
            {
                UserData = new UserData();
                var json = JsonUtility.ToJson(UserData, true);
                Debug.Log(json);
                File.WriteAllText(saveFilePath, json);
                return new UserData();
            }

            try
            {
                var json = File.ReadAllText(saveFilePath);
                var data = JsonUtility.FromJson<UserData>(json);
                return data;
            }
            catch (Exception e)
            {
                Debug.LogError("Failed to load user data: " + e.Message);
                return new UserData();
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
                Debug.Log(json);
                File.WriteAllText(saveFilePath, json);
            }
            catch (Exception e)
            {
                Debug.LogError("Failed to save user data: " + e.Message);
            }

            Debug.Log("Saved user data");
        }
    }
}
