using System.Collections.Generic;
using Cysharp.Text;
using Project.Scripts.Extensions;
using Project.Scripts.Model;
using UnityEngine.AddressableAssets;

namespace Project.Scripts.Repository.ModelRepository
{
    public class ModelRepositoryBase
    {
        protected string dataName = "";
        protected string DataAddress 
            => ZString.Format("{0}/{1}.asset", GamePath.DataStorepath, dataName);

        protected UserModel UserModel => UserModelRepository.Instance.Get();

        protected T LoadDataObject<T>()
        {
            return Addressables.LoadAssetAsync<T>(DataAddress).WaitForCompletion();
        }
    }
}
