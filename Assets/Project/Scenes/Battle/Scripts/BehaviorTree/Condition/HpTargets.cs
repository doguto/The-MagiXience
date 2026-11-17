using Project.Scenes.Battle.Scripts.Model.Entity;

namespace Project.Scenes.Battle.Scripts.BehaviorTree
{
    static class HpTargets
    {
        public static EnemyEntityModel Resolve(BattleContext ctx, HpTarget target) =>
            target == HpTarget.Self ? ctx.Self?.Model : ctx.Services.Boss?.Model;
    }
}
