using System.Collections.Generic;
using Project.Scripts.Extensions;
using Project.Scripts.Infra;
using Project.Scripts.Model;

namespace Project.Scripts.Repository.ModelRepository
{
    public class SoundModelRepository : ModelRepositoryBase
    {
        readonly List<BgmData> bgmData = new();
        readonly string bgmDataAddress = "Assets/Project/DataStore/BgmData.asset";
        readonly List<BgmModel> bgmModels = new();
        List<SeModel> seModels = new();

        public SoundModelRepository()
        {
            bgmData = LoadBgmData();
        }

        public BgmModel GetBgmModel(SceneType sceneType, BgmType bgmType = BgmType.Default)
        {
            var bgmModel = bgmModels.Find(x => x.BgmData.bgmType == bgmType && x.BgmData.sceneType == sceneType);
            if (bgmModel != null) return bgmModel;

            var foundBgmData = bgmData.Find(x => x.sceneType == sceneType && x.bgmType == bgmType);
            if (foundBgmData == null) return null;

            var newModel = new BgmModel(foundBgmData);
            bgmModels.Add(newModel);
            return newModel;
        }

        // サウンドデータをメモリにずっと持っておくと重そうなため、リフレッシュ出来るようにする
        public bool RefreshBgmModel(SceneType sceneType, BgmType bgmType)
        {
            var bgmModel = bgmModels.Find(x => x.BgmData.bgmType == bgmType);
            return bgmModels.Remove(bgmModel);
        }

        List<BgmData> LoadBgmData()
        {
            var dataObject = LoadDataObject<BgmDataObject>();
            return dataObject.bgmData;
        }
    }
}
