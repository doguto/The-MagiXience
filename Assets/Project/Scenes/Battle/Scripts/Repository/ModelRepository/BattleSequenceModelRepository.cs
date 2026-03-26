using System;
using System.Collections.Generic;
using Project.Scenes.Battle.Scripts.Model;
using Project.Scenes.Battle.Scripts.Model.Entity;
using Project.Scenes.Battle.Scripts.Model.ExitCondition;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Project.Scenes.Battle.Scripts.Repository.ModelRepository
{
    public class BattleSequenceModelRepository
    {
        readonly Dictionary<string, BattleSequenceAsset> cache = new();
        readonly IEnemyTracker enemyTracker;
        Func<EntityBase> getBossModel;
        Func<AudioSource> getBgmAudioSource;

        public BattleSequenceModelRepository(IEnemyTracker enemyTracker)
        {
            this.enemyTracker = enemyTracker;
        }

        public void SetBossModelProvider(Func<EntityBase> provider)
        {
            getBossModel = provider;
        }

        public void SetBgmAudioSourceProvider(Func<AudioSource> provider)
        {
            getBgmAudioSource = provider;
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

            return new BattleSequenceModel(asset.Situation, models, asset.BossPrefab, asset.BossSpawnPosition, asset.BossEntranceMovement);
        }

        public BattlePhaseModelBase CreatePhaseModel(BattlePhaseDefinition definition)
        {
            InjectDependencies(definition.ExitConditionConfig);
            return definition.ExitConditionConfig.CreatePhaseModel(definition, enemyTracker, getBossModel);
        }

        void InjectDependencies(IExitConditionConfig config)
        {
            if (config is BgmPositionExitConditionConfig bgmConfig)
            {
                bgmConfig.GetBgmAudioSource = getBgmAudioSource;
            }
            else if (config is CompositeExitConditionConfig compositeConfig)
            {
                foreach (var inner in compositeConfig.Conditions)
                {
                    InjectDependencies(inner);
                }
            } 
        }
    }
}
