using System;
using Project.Scenes.Battle.Scripts.Runtime;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.BehaviorTree
{
    [Serializable, NodeInfo("合図を送る。次のフレームの頭に宛先の敵・弾へ届き、Signaled 条件で受け取れる", "Action")]
    public sealed class SendSignal : ActionNode
    {
        [SerializeField] string key;
        [SerializeField] SignalTarget target = SignalTarget.Spawned;
        [SerializeField, ShowWhen(nameof(target), SignalTarget.Group)] string group;

        public override string Summary
        {
            get
            {
                var to = target switch
                {
                    SignalTarget.Spawned => "自分が出したもの",
                    SignalTarget.Group => $"グループ {group}",
                    _ => "全部",
                };
                return string.IsNullOrEmpty(key) ? "(合図未設定)" : $"合図 {key} → {to}";
            }
        }

        protected override NodeStatus OnTick(BattleContext ctx)
        {
            ctx.World.SendSignal(ctx.Self, key, target, group);
            return NodeStatus.Success;
        }
    }
}
