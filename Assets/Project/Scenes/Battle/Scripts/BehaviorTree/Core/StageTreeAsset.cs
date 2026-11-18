using System;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.BehaviorTree
{
    // ステージの進行。Self はいない
    [CreateAssetMenu(fileName = "StageTree", menuName = "Battle/Behavior Tree/Stage")]
    public sealed class StageTreeAsset : BehaviorTreeAsset
    {
        public override bool Accepts(Type nodeType) => !RequiresEnemy(nodeType);
    }
}
