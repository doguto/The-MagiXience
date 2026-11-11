using System.Collections.Generic;

namespace Project.Scenes.Battle.Scripts.Model.PhaseNode
{
    public interface IPhaseNode
    {
        string Label { get; }

        // 遅延評価。抽選や終了条件の判定は、そのノードに到達した時点で行われる
        IEnumerable<BattlePhaseDefinition> Enumerate(PhaseNodeContext context);
    }
}
