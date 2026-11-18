using System;
using Project.Scripts.Extensions;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.BehaviorTree
{
    [Serializable, NodeInfo("PlayScenario の子に置く。会話がこのコマンドの行に来たら子を実行する", "Flow")]
    public sealed class OnCommand : DecoratorNode
    {
        [SerializeField, Tooltip("シナリオの function 名 (例: SpawnBoss)")] string command;

        public string Command
        {
            get => command;
            set => command = value;
        }

        public override string Summary => string.IsNullOrEmpty(command) ? "(コマンド未設定)" : "On " + command;

        protected override NodeStatus OnTick(BattleContext ctx) => Child.Tick(ctx);
        protected override void OnAbort(BattleContext ctx) => Child.Abort(ctx);
    }
}
