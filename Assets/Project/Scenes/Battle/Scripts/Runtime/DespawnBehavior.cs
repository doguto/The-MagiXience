using Project.Scenes.Battle.Scripts.BehaviorTree;

namespace Project.Scenes.Battle.Scripts.Runtime
{
    // 消えるときに 1 回だけ実行するツリー (敵・弾で共通)
    public struct DespawnBehavior
    {
        public BehaviorTreeAsset Tree;
        public DespawnReason Reasons;

        public void Run(IAgent self, DespawnReason reason)
        {
            var world = self.World;
            if (Tree == null || world == null || (Reasons & reason) == 0) return;
            var ctx = new BattleContext(world, self);
            var root = world.Trees.Get(Tree, null);
            try
            {
                root.Tick(ctx);
            }
            catch (System.Exception e)
            {
                UnityEngine.Debug.LogException(e);
            }
            world.Trees.Return(Tree, null, root, ctx);
        }
    }
}
