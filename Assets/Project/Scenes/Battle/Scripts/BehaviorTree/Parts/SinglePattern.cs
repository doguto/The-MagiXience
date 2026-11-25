using System;
using System.Collections.Generic;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.BehaviorTree
{
    [Serializable]
    public sealed class SinglePattern : ShotPattern
    {
        public override void Build(Vector2 baseDirection, List<Shot> shots) => shots.Add(new Shot { Direction = baseDirection });
        public override string Summary => "単発";
    }
}
