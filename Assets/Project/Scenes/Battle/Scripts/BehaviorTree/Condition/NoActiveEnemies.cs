using System;

namespace Project.Scenes.Battle.Scripts.BehaviorTree
{
    // ---------- 条件 ----------

    [Serializable]
    public sealed class NoActiveEnemies : Condition
    {
        public override string Summary => "敵が全滅";
        public override bool Evaluate(BattleContext ctx) => ctx.Services.Spawner == null || ctx.Services.Spawner.ActiveEnemyCount == 0;
    }
}
