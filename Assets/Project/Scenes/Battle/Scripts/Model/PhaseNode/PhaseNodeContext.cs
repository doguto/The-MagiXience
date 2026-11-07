using System;
using System.Collections.Generic;
using Project.Scenes.Battle.Scripts.Model.Entity;
using Project.Scenes.Battle.Scripts.Model.ExitCondition;

namespace Project.Scenes.Battle.Scripts.Model.PhaseNode
{
    // シーケンス1回の再生ごとに作り直す。ノード自体はアセット上で共有されるため、実行時の状態はここに持つ
    public class PhaseNodeContext
    {
        readonly Func<IExitConditionConfig, BattlePhaseModelBase> watcherFactory;
        readonly Action interruptCurrentPhase;
        readonly Func<EntityBase> getBossModel;
        readonly Dictionary<RandomPhaseNode, int> lastPickedIndices = new();

        public PhaseNodeContext(Func<IExitConditionConfig, BattlePhaseModelBase> watcherFactory, Action interruptCurrentPhase, Func<EntityBase> getBossModel = null)
        {
            this.watcherFactory = watcherFactory;
            this.interruptCurrentPhase = interruptCurrentPhase;
            this.getBossModel = getBossModel;
        }

        public EntityBase BossModel => getBossModel?.Invoke();

        public BattlePhaseModelBase CreateWatcher(IExitConditionConfig config) => watcherFactory?.Invoke(config);

        public void InterruptCurrentPhase() => interruptCurrentPhase?.Invoke();

        public int GetLastPickedIndex(RandomPhaseNode node) => lastPickedIndices.TryGetValue(node, out var index) ? index : -1;

        public void SetLastPickedIndex(RandomPhaseNode node, int index) => lastPickedIndices[node] = index;
    }
}
