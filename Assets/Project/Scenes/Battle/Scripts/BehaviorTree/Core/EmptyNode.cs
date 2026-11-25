namespace Project.Scenes.Battle.Scripts.BehaviorTree
{
    // 子が無いデコレータや、ツリーが空のときの代わり
    sealed class EmptyNode : Node
    {
        protected override NodeStatus OnTick(BattleContext ctx) => NodeStatus.Success;
    }
}
