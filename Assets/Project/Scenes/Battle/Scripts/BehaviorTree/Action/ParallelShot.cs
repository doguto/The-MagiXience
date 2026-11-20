using System;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.BehaviorTree
{
    [Serializable, NodeInfo("同じ向きの弾を、向きと直角に等間隔で並べて同時に撃つ", "Action/Attack")]
    public sealed class ParallelShot : ShotNode
    {
        [SerializeField] ParallelPattern pattern = new();
        protected override ShotPattern Pattern => pattern;
    }
}
