using System;
using System.Collections.Generic;
using Project.Scripts.Model;
using Project.Scripts.Infra;
using UnityEngine.AddressableAssets;

namespace Project.Scripts.Repository.ModelRepository
{
    public class StageModelRepository : ModelRepositoryBase
    {
        public static StageModelRepository Instance { get; } = new();

        readonly List<StageData> stageData;
        readonly List<StageModel> stageModels = new();
        
        readonly UserModel userModel;

        public StageModelRepository()
        {
            userModel = UserModelRepository.Instance.Get();

            dataName = "StageData";
            stageData = LoadData();

            foreach (var data in stageData)
            {
                var stageNumber = data.stageNumber;
                stageModels.Add(
                    new StageModel(userModel, data, UserModel.IsOpenedStage(stageNumber), UserModel.IsClearedStage(stageNumber))
                );
            }
        }

        public StageModel GetByStageNumber(int stageNumber)
        {
            var model = stageModels.Find(m => m.StageData.stageNumber == stageNumber);
            if (model != null) return model;

            var data = stageData.Find(m => m.stageNumber == stageNumber);
            if (data == null) throw new Exception($"StageId {stageNumber} のデータが存在しません.");

            var newModel = new StageModel(userModel, data, UserModel.IsOpenedStage(stageNumber), UserModel.IsClearedStage(stageNumber));
            stageModels.Add(newModel);
            return newModel;
        }

        public List<StageModel> GetAll() => stageModels;

        public void Clear()
        {
            stageModels.Clear();
        }

        List<StageData> LoadData()
        {
            var dataObject = LoadDataObject<StageDataObject>();
            return dataObject.stageData;
        }
    }
}
