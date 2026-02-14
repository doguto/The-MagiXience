using System;
using System.Collections.Generic;
using Project.Scripts.Extensions;
using Project.Scripts.Infra;
using Project.Scripts.Model;

namespace Project.Scripts.Repository.ModelRepository
{
    public class SoundModelRepository : ModelRepositoryBase
    {
        public static SoundModelRepository Instance { get; } = new();

        readonly List<BgmData> bgmData = new();
        readonly List<SeData> seData = new();
        readonly List<BgmModel> bgmModels = new();
        readonly List<SeModel> seModels = new();

        public SoundModelRepository()
        {
            bgmData = LoadBgmData();
            seData = LoadSeData();
        }

        public BgmModel GetBgmModel(SceneType sceneType, BgmType bgmType = BgmType.Default)
        {
            var bgmModel = bgmModels.Find(x => x.BgmData.bgmType == bgmType && x.BgmData.sceneType == sceneType);
            if (bgmModel != null) return bgmModel;

            var foundBgmData = bgmData.Find(x => x.sceneType == sceneType && x.bgmType == bgmType);
            if (foundBgmData == null) throw new Exception($"BgmData: {sceneType.ToString()}_{bgmType.ToString()} が見つかりません");

            var newModel = new BgmModel(foundBgmData);
            bgmModels.Add(newModel);
            return newModel;
        }

        public SeModel GetSeModel(SeType seType)
        {
            var seModel = seModels.Find(x => x.SeData.seType == seType);
            if (seModel != null) return seModel;

            var foundSeData = seData.Find(x => x.seType == seType);
            if (foundSeData == null) throw new Exception($"SeData: {seType.ToString()} が見つかりません");

            var newModel = new SeModel(foundSeData);
            seModels.Add(newModel);
            return newModel;
        }

        // BGMデータをメモリにずっと持っておくと重そうなため、リフレッシュ出来るようにする
        // 逆にSEは使用頻度が高そうなためリフレッシュせずメモリにずっと置く
        public bool RefreshBgmModel(SceneType sceneType, BgmType bgmType)
        {
            var bgmModel = bgmModels.Find(x => x.BgmData.bgmType == bgmType);
            return bgmModels.Remove(bgmModel);
        }

        List<BgmData> LoadBgmData()
        {
            dataName = "BgmData";
            var dataObject = LoadDataObject<BgmDataObject>();
            return dataObject.bgmData;
        }

        List<SeData> LoadSeData()
        {
            dataName = "SeData";
            var dataObject = LoadDataObject<SeDataObject>();
            return dataObject.seData;
        }
    }
}
