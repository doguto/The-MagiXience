using System;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.BehaviorTree
{
    // どの種類のツリーからも SubTree で使えるツリー (攻撃など)。Self の種類に依存するノードは置けない
    [CreateAssetMenu(fileName = "CommonTree", menuName = "Battle/Behavior Tree/Common")]
    public sealed class CommonTreeAsset : BehaviorTreeAsset
    {
        public override bool Accepts(Type nodeType) => !RequiresEnemy(nodeType);

        public override bool CanUse(BehaviorTreeAsset subTree) => subTree is CommonTreeAsset;
    }
}
