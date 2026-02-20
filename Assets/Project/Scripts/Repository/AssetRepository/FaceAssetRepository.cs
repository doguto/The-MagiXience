using System.Collections.Generic;
using System.Linq;
using Cysharp.Text;
using Project.Scripts.Extensions;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Project.Scripts.Repository.AssetRepository
{
    public class FaceAssetRepository : AssetRepositoryBase
    {
        public Dictionary<string, Sprite> LoadAll(string characterName)
        {
            var faceDictionary = new Dictionary<string, Sprite>();

            var faceTypes = new List<string> { "Default", "Smile", "Angry", "Additional", "Crazy_Default", "Crazy_Additional" };
            
            var keys = faceTypes.Select(type => 
                ZString.Format("{0}/Character/{1}/Face/{1}_{2}_Face.png",
                    GamePath.TexturesPath, 
                    characterName, 
                    type)
            ).ToList();

            var loadOperation = Addressables.LoadAssetsAsync<Sprite>(
                keys,
                sprite =>
                {
                    var spriteName = sprite.name;
                    // スプライト名の形式: {characterName}_{expressionType}_Face
                    // characterNameの長さ + 1（アンダースコア）を開始位置とする
                    var charNameLength = characterName.Length + 1;
                    var faceIndex = spriteName.LastIndexOf("_Face");
                    if (faceIndex > charNameLength)
                    {
                        // expressionTypeを抽出: charNameLengthからfaceIndexまでの部分
                        var expressionKey = spriteName.Substring(charNameLength, faceIndex - charNameLength);
                        if (!faceDictionary.ContainsKey(expressionKey))
                        {
                            faceDictionary.Add(expressionKey, sprite);
                        }
                    }
                },
                Addressables.MergeMode.Union
            );

            loadOperation.WaitForCompletion();
            
            return faceDictionary;
        }
    }
}
