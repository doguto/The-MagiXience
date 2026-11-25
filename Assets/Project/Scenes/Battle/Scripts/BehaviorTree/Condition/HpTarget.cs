using UnityEngine;

namespace Project.Scenes.Battle.Scripts.BehaviorTree
{
    public enum HpTarget
    {
        [Tooltip("このツリーを持つ敵")] Self,
        [Tooltip("ボス")] Boss,
    }
}
