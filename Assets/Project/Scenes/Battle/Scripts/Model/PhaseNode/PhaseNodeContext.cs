using System;
using System.Collections.Generic;
using Project.Scenes.Battle.Scripts.Model.ExitCondition;

namespace Project.Scenes.Battle.Scripts.Model.PhaseNode
{
    // シーケンス1回の再生ごとに作り直す。ノード自体はアセット上で共有されるため、実行時の状態はここに持つ
    public class PhaseNodeContext
    {
        readonly Func<IExitConditionConfig, BattlePhaseModelBase> watcherFactory;
        readonly Action interruptCurrentPhase;
        readonly Dictionary<RandomPhaseNode, int> lastPickedIndices = new();

        public PhaseNodeContext(Func<IExitConditionConfig, BattlePhaseModelBase> watcherFactory, Action interruptCurrentPhase)
        {
            this.watcherFactory = watcherFactory;
            this.interruptCurrentPhase = interruptCurrentPhase;
        }

        public BattlePhaseModelBase CreateWatcher(IExitConditionConfig config) => watcherFactory?.Invoke(config);

        public void InterruptCurrentPhase() => interruptCurrentPhase?.Invoke();

        public int GetLastPickedIndex(RandomPhaseNode node) => lastPickedIndices.TryGetValue(node, out var index) ? index : -1;

        public void SetLastPickedIndex(RandomPhaseNode node, int index) => lastPickedIndices[node] = index;
    }
}
