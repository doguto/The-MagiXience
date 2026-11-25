using System;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.BehaviorTree
{
    [Serializable, NodeInfo("円周上に 1 発ずつ並べ、最後の 1 発のあと全弾同時に動き出す", "Action/Attack")]
    public sealed class SequentialCircleShot : ShotNode
    {
        [SerializeField] SequentialCirclePattern pattern = new();
        protected override ShotPattern Pattern => pattern;
    }
}
