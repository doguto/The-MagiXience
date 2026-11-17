using System;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.BehaviorTree
{
    [Serializable, NodeInfo("扇状に同時に撃つ。奇数は中央に 1 発、偶数は中央を空ける", "Action/Attack")]
    public sealed class NWayShot : ShotNode
    {
        [SerializeField] NWayPattern pattern = new();
        protected override ShotPattern Pattern => pattern;
    }
}
