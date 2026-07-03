using System;
using System.Collections.Generic;
using Project.Scripts.Infra;
using Project.Scripts.Model;

namespace Project.Scripts.Repository.ModelRepository
{
    public class CharacterModelRepository : ModelRepositoryBase
    {
        public static CharacterModelRepository Instance { get; } = new();

        readonly List<CharacterData> characterData;
        readonly List<CharacterModel> characterModels = new();

        public CharacterModelRepository()
        {
            dataName = "CharacterData";
            characterData = LoadData();
            foreach (var data in characterData)
            {
                characterModels.Add(new CharacterModel(data));
            }
        }

        public CharacterModel GetById(int characterId)
        {
            var model = characterModels.Find(m => m.CharacterData.id == characterId);
            if (model != null) return model;

            var data = characterData.Find(m => m.id == characterId);
            if (data == null) throw new Exception($"CharacterId {characterId} のキャラクターデータは存在しません.");

            var newModel = new CharacterModel(data);
            characterModels.Add(newModel);

            return newModel;
        }

        public CharacterModel GetByName(string name)
        {
            var model = characterModels.Find(m => m.Name == name);
            if (model != null) return model;

            var data = characterData.Find(m => m.name == name);
            if (data == null) throw new Exception($"キャラクター名 {name} のキャラクターデータは存在しません.");

            var newModel = new CharacterModel(data);
            characterModels.Add(newModel);
            
            return newModel;
        }

        public List<CharacterModel> GetAll() => characterModels;

        public void Refresh()
        {
            characterData.Clear();
        }

        List<CharacterData> LoadData()
        {
            var dataObject = LoadDataObject<CharacterDataObject>();
            return dataObject.characterData;
        }
    }
}
