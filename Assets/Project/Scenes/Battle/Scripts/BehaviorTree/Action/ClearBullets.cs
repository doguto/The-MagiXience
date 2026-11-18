using System;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.BehaviorTree
{
    [Serializable, NodeInfo("画面の弾・警告線・ビームを消す。敵も消せる", "Action/Stage")]
    public sealed class ClearBullets : ActionNode
    {
        [SerializeField] bool clearEnemies;

        public override string Summary => clearEnemies ? "弾と敵を消す" : "弾を消す";

        protected override NodeStatus OnTick(BattleContext ctx)
        {
            ctx.Services.Bullets.Clear();
            ctx.Services.Clearer?.ClearAllBeams();
            if (clearEnemies) ctx.Services.Clearer?.ClearAllEnemies();
            return NodeStatus.Success;
        }
    }
}
