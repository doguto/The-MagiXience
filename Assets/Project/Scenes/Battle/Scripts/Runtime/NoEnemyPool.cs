using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Runtime
{
    // Prefab に付けると、その敵をプールで使い回さず毎回生成・破棄する
    [DisallowMultipleComponent]
    public sealed class NoEnemyPool : MonoBehaviour
    {
    }
}
