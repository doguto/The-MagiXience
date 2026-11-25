using System;
using Project.Scripts.Extensions;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.BehaviorTree
{
    // ---------- BGM ----------

    [Serializable, NodeInfo("このステージの BGM を流す (曲名の表示はシーン側)", "Action/Stage")]
    public sealed class PlayBgm : ActionNode
    {
        [SerializeField] BgmType bgm = BgmType.BattleWay;

        public override string Summary => bgm.ToString();

        protected override NodeStatus OnTick(BattleContext ctx)
        {
            ctx.Services.PlayBgm?.Invoke(bgm);
            return NodeStatus.Success;
        }
    }
}
