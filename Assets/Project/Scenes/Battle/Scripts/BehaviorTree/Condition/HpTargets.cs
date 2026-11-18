using Project.Scenes.Battle.Scripts.Model.Entity;
using Project.Scenes.Battle.Scripts.Runtime;

namespace Project.Scenes.Battle.Scripts.BehaviorTree
{
    static class HpTargets
    {
        public static EnemyEntityModel Resolve(BattleContext ctx, HpTarget target) =>
            target == HpTarget.Self ? (ctx.Self as EnemyAgent)?.Model : ctx.Services.Boss?.Model;
    }
}
