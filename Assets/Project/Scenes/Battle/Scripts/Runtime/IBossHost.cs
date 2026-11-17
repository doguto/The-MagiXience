using Project.Scenes.Battle.Scripts.BehaviorTree;

namespace Project.Scenes.Battle.Scripts.Runtime
{
    // ボスの生成とボス戦の開始。生成は会話の途中、戦闘開始は会話の後になるので分けている
    public interface IBossHost
    {
        BossDefinition Current { get; }
        bool Spawn(BossDefinition definition);
        BattleActor StartBattle(BehaviorTreeAsset tree);
        // 撃破演出まで終わったら true
        bool IsDefeated { get; }
    }
}
