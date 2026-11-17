using System;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.BehaviorTree
{
    [Serializable, NodeInfo("扇状に 1 発ずつ並べ、最後の 1 発のあと全弾同時に動き出す", "Action/Attack")]
    public sealed class SequentialFanShot : ShotNode
    {
        [SerializeField] SequentialFanPattern pattern = new();
        protected override ShotPattern Pattern => pattern;
    }
}
