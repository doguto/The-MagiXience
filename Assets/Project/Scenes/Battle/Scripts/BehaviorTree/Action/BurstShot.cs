using System;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.BehaviorTree
{
    [Serializable, NodeInfo("同じ扇を間隔をあけて何回か撃つ。発射位置の半径は 1 回目から最後へ線形に変わる", "Action/Attack")]
    public sealed class BurstShot : ShotNode
    {
        [SerializeField] BurstPattern pattern = new();
        protected override ShotPattern Pattern => pattern;
    }
}
