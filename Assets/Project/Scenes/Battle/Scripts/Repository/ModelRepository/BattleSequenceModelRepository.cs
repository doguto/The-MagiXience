using System;
using System.Collections.Generic;
using Project.Scenes.Battle.Scripts.Model;
using UnityEngine.AddressableAssets;

namespace Project.Scenes.Battle.Scripts.Repository.ModelRepository
{
    public class BattleSequenceModelRepository
    {
        readonly Dictionary<string, BattleSequenceAsset> cache = new();
        readonly IEnemyTracker enemyTracker;

        public BattleSequenceModelRepository(IEnemyTracker enemyTracker)
        {
            this.enemyTracker = enemyTracker;
        }

        public BattleSequenceModel Load(string address)
        {
            if (string.IsNullOrEmpty(address))
            {
                throw new ArgumentException("Sequence address is empty.", nameof(address));
            }

            if (!cache.TryGetValue(address, out var asset))
            {
                asset = Addressables.LoadAssetAsync<BattleSequenceAsset>(address).WaitForCompletion();
                cache[address] = asset;
            }

            var models = new List<BattlePhaseModelBase>(asset.Phases.Count);
            foreach (var definition in asset.Phases)
            {
                models.Add(CreatePhaseModel(definition));
            }

            return new BattleSequenceModel(asset.Situation, models);
        }
        
        public BattlePhaseModelBase CreatePhaseModel(BattlePhaseDefinition definition)
        {
            return definition.ExitConditionConfig.CreatePhaseModel(definition, enemyTracker);
        }
    }
}
