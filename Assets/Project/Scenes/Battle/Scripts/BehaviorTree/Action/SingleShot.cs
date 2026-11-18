using System;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.BehaviorTree
{
    [Serializable, NodeInfo("基準の向きに 1 発撃つ", "Action/Attack")]
    public sealed class SingleShot : ShotNode
    {
        [SerializeField] SinglePattern pattern = new();
        protected override ShotPattern Pattern => pattern;
    }
}
